package si.mentis.eprevzemmobile.data.security

import kotlinx.cinterop.COpaquePointerVar
import kotlinx.cinterop.CPointer
import kotlinx.cinterop.ExperimentalForeignApi
import kotlinx.cinterop.addressOf
import kotlinx.cinterop.alloc
import kotlinx.cinterop.allocArray
import kotlinx.cinterop.convert
import kotlinx.cinterop.get
import kotlinx.cinterop.memScoped
import kotlinx.cinterop.ptr
import kotlinx.cinterop.reinterpret
import kotlinx.cinterop.ULongVar
import kotlinx.cinterop.usePinned
import platform.CoreCrypto.CCKeyDerivationPBKDF
import platform.CoreCrypto.CCRandomGenerateBytes
import platform.CoreCrypto.kCCAlgorithmAES
import platform.CoreCrypto.kCCBlockSizeAES128
import platform.CoreCrypto.kCCDecrypt
import platform.CoreCrypto.kCCEncrypt
import platform.CoreCrypto.kCCKeySizeAES256
import platform.CoreCrypto.kCCOptionECBMode
import platform.CoreCrypto.kCCPRFHmacAlgSHA256
import platform.CoreCrypto.kCCPBKDF2
import platform.CoreCrypto.kCCSuccess
import platform.CoreCrypto.CCCrypt
import platform.CoreFoundation.CFDataRef
import platform.CoreFoundation.CFDictionaryRef
import platform.CoreFoundation.CFErrorRefVar
import platform.Foundation.NSData
import platform.Foundation.NSMutableDictionary
import platform.Foundation.NSString
import platform.Security.SecKeyCopyExternalRepresentation
import platform.Security.SecKeyCreateRandomKey
import platform.Security.SecKeyCreateSignature
import platform.Security.SecKeyCreateWithData
import platform.Security.errSecSuccess
import platform.Security.kSecAttrKeyClass
import platform.Security.kSecAttrKeyClassPrivate
import platform.Security.kSecAttrKeySizeInBits
import platform.Security.kSecAttrKeyType
import platform.Security.kSecAttrKeyTypeECSECPrimeRandom
import platform.Security.kSecPrivateKeyAttrs
import platform.Security.kSecAttrIsPermanent
import platform.Security.kSecKeyAlgorithmECDSASignatureMessageX962SHA256
import platform.Security.kSecPublicKeyAttrs
import platform.posix.uint8_tVar
import kotlin.experimental.xor

private const val GCM_TAG_SIZE = 16
private const val GCM_BLOCK_SIZE = 16

@OptIn(ExperimentalForeignApi::class)
actual class SecurityCrypto actual constructor() {
    actual fun randomBytes(size: Int): ByteArray {
        val bytes = ByteArray(size)
        if (size == 0) return bytes
        bytes.usePinned { pinned ->
            check(CCRandomGenerateBytes(pinned.addressOf(0), size.convert()) == kCCSuccess) {
                "Unable to generate secure random bytes"
            }
        }
        return bytes
    }

    actual fun generateEcdsaKeyPair(): EcdsaKeyPair = memScoped {
        val error = alloc<CFErrorRefVar>()
        val attributes = NSMutableDictionary().apply {
            setObject(kSecAttrKeyTypeECSECPrimeRandom, forKey = kSecAttrKeyType as NSString)
            setObject(256, forKey = kSecAttrKeySizeInBits as NSString)
            setObject(
                NSMutableDictionary().apply {
                    setObject(false, forKey = kSecAttrIsPermanent as NSString)
                },
                forKey = kSecPrivateKeyAttrs as NSString,
            )
            setObject(
                NSMutableDictionary().apply {
                    setObject(false, forKey = kSecAttrIsPermanent as NSString)
                },
                forKey = kSecPublicKeyAttrs as NSString,
            )
        }
        val privateKey = SecKeyCreateRandomKey(attributes.asCfDictionary(), error.ptr)
            ?: error("Unable to generate P-256 key pair")

        val privateData = SecKeyCopyExternalRepresentation(privateKey, error.ptr) as NSData?
            ?: error("Unable to export private key")
        val publicKey = platform.Security.SecKeyCopyPublicKey(privateKey)
            ?: error("Unable to export public key")
        val publicData = SecKeyCopyExternalRepresentation(publicKey, error.ptr) as NSData?
            ?: error("Unable to export public key bytes")

        val publicDer = p256SubjectPublicKeyInfo(publicData.toByteArray())
        EcdsaKeyPair(
            publicKeyPem = publicDer.toPem("PUBLIC KEY"),
            privateKeyBytes = privateData.toByteArray(),
        )
    }

    actual fun deriveAesKey(pin: String, salt: ByteArray, iterations: Int): ByteArray {
        val password = pin.encodeToByteArray()
        val derivedKey = ByteArray(kCCKeySizeAES256.toInt())
        val status = password.usePinned { passwordPinned ->
            salt.usePinned { saltPinned ->
                derivedKey.usePinned { keyPinned ->
                    CCKeyDerivationPBKDF(
                        kCCPBKDF2,
                        pin,
                        password.size.convert(),
                        saltPinned.addressOf(0).reinterpret<uint8_tVar>(),
                        salt.size.convert(),
                        kCCPRFHmacAlgSHA256,
                        iterations.convert(),
                        keyPinned.addressOf(0).reinterpret<uint8_tVar>(),
                        derivedKey.size.convert(),
                    )
                }
            }
        }
        check(status == kCCSuccess) { "PBKDF2 failed with status $status" }
        return derivedKey
    }

    actual fun encryptAesGcm(key: ByteArray, plaintext: ByteArray): EncryptedPayload {
        val nonce = randomBytes(size = 12)
        val encrypted = aesGcmCrypt(key = key, nonce = nonce, input = plaintext, encrypt = true)
        return EncryptedPayload(
            ciphertext = encrypted.ciphertext + encrypted.tag,
            nonce = nonce,
        )
    }

    actual fun decryptAesGcm(key: ByteArray, payload: EncryptedPayload): ByteArray {
        require(payload.ciphertext.size >= GCM_TAG_SIZE) { "Invalid GCM payload" }
        val ciphertext = payload.ciphertext.copyOfRange(0, payload.ciphertext.size - GCM_TAG_SIZE)
        val tag = payload.ciphertext.copyOfRange(payload.ciphertext.size - GCM_TAG_SIZE, payload.ciphertext.size)
        return aesGcmCrypt(key = key, nonce = payload.nonce, input = ciphertext, encrypt = false, expectedTag = tag).ciphertext
    }

    actual fun signEcdsa(privateKeyBytes: ByteArray, challenge: ByteArray): ByteArray = memScoped {
        val error = alloc<CFErrorRefVar>()
        val attributes = NSMutableDictionary().apply {
            setObject(kSecAttrKeyTypeECSECPrimeRandom, forKey = kSecAttrKeyType as NSString)
            setObject(kSecAttrKeyClassPrivate, forKey = kSecAttrKeyClass as NSString)
            setObject(256, forKey = kSecAttrKeySizeInBits as NSString)
        }
        val privateKey = SecKeyCreateWithData(privateKeyBytes.toNSData().asCfData(), attributes.asCfDictionary(), error.ptr)
            ?: error("Unable to import private key")
        val signature = SecKeyCreateSignature(
            privateKey,
            kSecKeyAlgorithmECDSASignatureMessageX962SHA256,
            challenge.toNSData().asCfData(),
            error.ptr,
        ) as NSData? ?: error("Unable to sign challenge")
        signature.toByteArray()
    }

    private fun aesGcmCrypt(
        key: ByteArray,
        nonce: ByteArray,
        input: ByteArray,
        encrypt: Boolean,
        expectedTag: ByteArray? = null,
    ): GcmResult {
        require(key.size == kCCKeySizeAES256.toInt()) { "AES-256 key must be 32 bytes" }
        require(nonce.size == 12) { "GCM nonce must be 12 bytes" }

        val hashSubkey = aesBlock(key, ByteArray(GCM_BLOCK_SIZE))
        val initialCounter = nonce + byteArrayOf(0, 0, 0, 1)
        val output = ByteArray(input.size)
        var counter = incrementCounter(initialCounter)
        var offset = 0

        while (offset < input.size) {
            val blockSize = minOf(GCM_BLOCK_SIZE, input.size - offset)
            val stream = aesBlock(key, counter)
            repeat(blockSize) { index ->
                output[offset + index] = input[offset + index] xor stream[index]
            }
            counter = incrementCounter(counter)
            offset += blockSize
        }

        val ciphertextForTag = if (encrypt) output else input
        val tagMask = aesBlock(key, initialCounter)
        val computedTag = ghash(hashSubkey, ciphertextForTag) xorBlock tagMask

        if (!encrypt) {
            check(expectedTag != null && constantTimeEquals(expectedTag, computedTag)) { "Invalid GCM authentication tag" }
        }

        return GcmResult(ciphertext = output, tag = computedTag)
    }

    private fun aesBlock(key: ByteArray, block: ByteArray): ByteArray {
        require(block.size == GCM_BLOCK_SIZE)
        val output = ByteArray(GCM_BLOCK_SIZE)
        val outputLength = memScoped {
            val outLength = allocArray<ULongVar>(1)
            val status = key.usePinned { keyPinned ->
                block.usePinned { blockPinned ->
                    output.usePinned { outputPinned ->
                        CCCrypt(
                            kCCEncrypt,
                            kCCAlgorithmAES,
                            kCCOptionECBMode,
                            keyPinned.addressOf(0),
                            key.size.convert(),
                            null,
                            blockPinned.addressOf(0),
                            block.size.convert(),
                            outputPinned.addressOf(0),
                            output.size.convert(),
                            outLength,
                        )
                    }
                }
            }
            check(status == kCCSuccess) { "AES block encryption failed with status $status" }
            outLength[0].toInt()
        }
        check(outputLength == GCM_BLOCK_SIZE) { "Unexpected AES block output length" }
        return output
    }
}

@OptIn(ExperimentalForeignApi::class)
private fun NSMutableDictionary.asCfDictionary(): CFDictionaryRef =
    this as CFDictionaryRef

@OptIn(ExperimentalForeignApi::class)
private fun NSData.asCfData(): CFDataRef =
    this as CFDataRef

private data class GcmResult(
    val ciphertext: ByteArray,
    val tag: ByteArray,
)

private infix fun ByteArray.xorBlock(other: ByteArray): ByteArray =
    ByteArray(size) { index -> this[index] xor other[index] }

private fun incrementCounter(counter: ByteArray): ByteArray {
    val next = counter.copyOf()
    for (index in 15 downTo 12) {
        next[index] = (next[index].toInt() + 1).toByte()
        if (next[index].toInt() != 0) break
    }
    return next
}

private fun ghash(hashSubkey: ByteArray, ciphertext: ByteArray): ByteArray {
    var y = ByteArray(GCM_BLOCK_SIZE)
    for (offset in ciphertext.indices step GCM_BLOCK_SIZE) {
        val block = ByteArray(GCM_BLOCK_SIZE)
        val blockSize = minOf(GCM_BLOCK_SIZE, ciphertext.size - offset)
        ciphertext.copyInto(block, endIndex = offset + blockSize, destinationOffset = 0, startIndex = offset)
        y = multiplyGf128(y xorBlock block, hashSubkey)
    }

    val lengthBlock = ByteArray(GCM_BLOCK_SIZE)
    val bitLength = ciphertext.size.toLong() * 8L
    for (index in 0 until 8) {
        lengthBlock[15 - index] = (bitLength ushr (index * 8)).toByte()
    }
    return multiplyGf128(y xorBlock lengthBlock, hashSubkey)
}

private fun multiplyGf128(x: ByteArray, y: ByteArray): ByteArray {
    var z = ByteArray(GCM_BLOCK_SIZE)
    var v = y.copyOf()
    repeat(128) { bitIndex ->
        if (((x[bitIndex / 8].toInt() ushr (7 - bitIndex % 8)) and 1) == 1) {
            z = z xorBlock v
        }
        val lsb = v[15].toInt() and 1
        shiftRightOne(v)
        if (lsb == 1) {
            v[0] = v[0] xor 0xe1.toByte()
        }
    }
    return z
}

private fun shiftRightOne(bytes: ByteArray) {
    var carry = 0
    for (index in bytes.indices) {
        val value = bytes[index].toInt() and 0xff
        bytes[index] = ((value ushr 1) or carry).toByte()
        carry = (value and 1) shl 7
    }
}

private fun constantTimeEquals(left: ByteArray, right: ByteArray): Boolean {
    if (left.size != right.size) return false
    var diff = 0
    for (index in left.indices) {
        diff = diff or (left[index].toInt() xor right[index].toInt())
    }
    return diff == 0
}

private fun p256SubjectPublicKeyInfo(x963PublicKey: ByteArray): ByteArray {
    val header = byteArrayOf(
        0x30, 0x59,
        0x30, 0x13,
        0x06, 0x07, 0x2a, 0x86.toByte(), 0x48, 0xce.toByte(), 0x3d, 0x02, 0x01,
        0x06, 0x08, 0x2a, 0x86.toByte(), 0x48, 0xce.toByte(), 0x3d, 0x03, 0x01, 0x07,
        0x03, 0x42, 0x00,
    )
    return header + x963PublicKey
}
