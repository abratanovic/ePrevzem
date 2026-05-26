package si.mentis.eprevzemmobile.data.security

import java.security.KeyFactory
import java.security.KeyPairGenerator
import java.security.SecureRandom
import java.security.Signature
import java.security.spec.ECGenParameterSpec
import java.security.spec.PKCS8EncodedKeySpec
import javax.crypto.Cipher
import javax.crypto.SecretKeyFactory
import javax.crypto.spec.GCMParameterSpec
import javax.crypto.spec.PBEKeySpec
import javax.crypto.spec.SecretKeySpec

actual class SecurityCrypto actual constructor() {
    private val secureRandom = SecureRandom()

    actual fun randomBytes(size: Int): ByteArray =
        ByteArray(size).also(secureRandom::nextBytes)

    actual fun generateEcdsaKeyPair(): EcdsaKeyPair {
        val generator = KeyPairGenerator.getInstance("EC")
        generator.initialize(ECGenParameterSpec("secp256r1"), secureRandom)
        val keyPair = generator.generateKeyPair()
        return EcdsaKeyPair(
            publicKeyPem = keyPair.public.encoded.toPem("PUBLIC KEY"),
            privateKeyBytes = keyPair.private.encoded,
        )
    }

    actual fun deriveAesKey(pin: String, salt: ByteArray, iterations: Int): ByteArray {
        val spec = PBEKeySpec(pin.toCharArray(), salt, iterations, 256)
        return SecretKeyFactory.getInstance("PBKDF2WithHmacSHA256")
            .generateSecret(spec)
            .encoded
    }

    actual fun encryptAesGcm(key: ByteArray, plaintext: ByteArray): EncryptedPayload {
        val nonce = randomBytes(size = 12)
        val cipher = Cipher.getInstance("AES/GCM/NoPadding")
        cipher.init(Cipher.ENCRYPT_MODE, SecretKeySpec(key, "AES"), GCMParameterSpec(128, nonce))
        return EncryptedPayload(
            ciphertext = cipher.doFinal(plaintext),
            nonce = nonce,
        )
    }

    actual fun decryptAesGcm(key: ByteArray, payload: EncryptedPayload): ByteArray {
        val cipher = Cipher.getInstance("AES/GCM/NoPadding")
        cipher.init(Cipher.DECRYPT_MODE, SecretKeySpec(key, "AES"), GCMParameterSpec(128, payload.nonce))
        return cipher.doFinal(payload.ciphertext)
    }

    actual fun signEcdsa(privateKeyBytes: ByteArray, challenge: ByteArray): ByteArray {
        val privateKey = KeyFactory.getInstance("EC").generatePrivate(PKCS8EncodedKeySpec(privateKeyBytes))
        val signature = Signature.getInstance("SHA256withECDSA")
        signature.initSign(privateKey, secureRandom)
        signature.update(challenge)
        return signature.sign()
    }
}
