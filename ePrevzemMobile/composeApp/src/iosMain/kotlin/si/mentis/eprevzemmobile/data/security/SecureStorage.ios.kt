package si.mentis.eprevzemmobile.data.security

import kotlinx.cinterop.COpaquePointerVar
import kotlinx.cinterop.ExperimentalForeignApi
import kotlinx.cinterop.addressOf
import kotlinx.cinterop.alloc
import kotlinx.cinterop.allocArray
import kotlinx.cinterop.convert
import kotlinx.cinterop.get
import kotlinx.cinterop.memScoped
import kotlinx.cinterop.ptr
import kotlinx.cinterop.usePinned
import platform.CoreFoundation.CFErrorRefVar
import platform.CoreFoundation.CFErrorRef
import platform.CoreFoundation.CFRelease
import platform.CoreFoundation.CFDictionaryRef
import platform.Foundation.NSData
import platform.Foundation.NSMutableDictionary
import platform.Foundation.NSString
import platform.Foundation.create
import platform.Security.SecAccessControlCreateWithFlags
import platform.Security.SecItemAdd
import platform.Security.SecItemCopyMatching
import platform.Security.SecItemDelete
import platform.Security.SecItemUpdate
import platform.Security.errSecItemNotFound
import platform.Security.errSecSuccess
import platform.Security.kSecAttrAccessControl
import platform.Security.kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly
import platform.Security.kSecAttrAccessibleWhenUnlockedThisDeviceOnly
import platform.Security.kSecAttrAccount
import platform.Security.kSecAttrService
import platform.Security.kSecClass
import platform.Security.kSecClassGenericPassword
import platform.Security.kSecReturnData
import platform.Security.kSecValueData
import platform.Security.kSecAttrSynchronizable
import platform.Security.kSecAccessControlBiometryCurrentSet
import platform.darwin.OSStatus

private const val SERVICE_NAME = "si.mentis.eprevzemmobile.secure-storage"
private const val BIOMETRIC_PREFIX = "biometric."

@OptIn(ExperimentalForeignApi::class)
actual class SecureStorage actual constructor() {
    actual suspend fun readString(key: String): String? =
        readKeychainString(key = key, prompt = null)

    actual suspend fun writeString(key: String, value: String) {
        writeKeychainString(key = key, value = value, biometricProtected = false)
    }

    actual suspend fun remove(key: String) {
        deleteKeychainString(key)
        deleteKeychainString(BIOMETRIC_PREFIX + key)
    }

    actual suspend fun readBiometricString(key: String): String? =
        readKeychainString(
            key = BIOMETRIC_PREFIX + key,
            prompt = "Potrdite identiteto za dostop do ključa",
        )

    actual suspend fun writeBiometricString(key: String, value: String) {
        writeKeychainString(key = BIOMETRIC_PREFIX + key, value = value, biometricProtected = true)
    }

    private fun readKeychainString(key: String, prompt: String?): String? = memScoped {
        val result = allocArray<COpaquePointerVar>(1)
        val query = baseQuery(key).apply {
            setObject(true, forKey = kSecReturnData as NSString)
        }
        val status = SecItemCopyMatching(query.asCfDictionary(), result)
        if (status == errSecItemNotFound) return null
        checkStatus(status, "read keychain item")

        val data = result[0] as NSData
        data.toByteArray().decodeToString()
    }

    private fun writeKeychainString(key: String, value: String, biometricProtected: Boolean) {
        val data = value.encodeToByteArray().toNSData()
        val attributes = baseAttributes(key, data, biometricProtected)

        val addStatus = SecItemAdd(attributes.asCfDictionary(), null)
        if (addStatus == errSecSuccess) return
        if (addStatus != platform.Security.errSecDuplicateItem) {
            checkStatus(addStatus, "write keychain item")
        }

        val updateStatus = SecItemUpdate(
            baseQuery(key).asCfDictionary(),
            NSMutableDictionary().apply {
                setObject(data, forKey = kSecValueData as NSString)
            }.asCfDictionary(),
        )
        checkStatus(updateStatus, "update keychain item")
    }

    private fun deleteKeychainString(key: String) {
        val status = SecItemDelete(baseQuery(key).asCfDictionary())
        if (status != errSecItemNotFound) {
            checkStatus(status, "delete keychain item")
        }
    }

    private fun baseQuery(key: String): NSMutableDictionary =
        NSMutableDictionary().apply {
            setObject(kSecClassGenericPassword, forKey = kSecClass as NSString)
            setObject(SERVICE_NAME, forKey = kSecAttrService as NSString)
            setObject(key, forKey = kSecAttrAccount as NSString)
            setObject(false, forKey = kSecAttrSynchronizable as NSString)
        }

    private fun baseAttributes(
        key: String,
        data: NSData,
        biometricProtected: Boolean,
    ): NSMutableDictionary = baseQuery(key).apply {
        setObject(data, forKey = kSecValueData as NSString)
        if (biometricProtected) {
            val accessControl = memScoped {
                val error = alloc<CFErrorRefVar>()
                SecAccessControlCreateWithFlags(
                    null,
                    kSecAttrAccessibleWhenUnlockedThisDeviceOnly,
                    kSecAccessControlBiometryCurrentSet,
                    error.ptr,
                )
            } ?: error("Unable to create biometric keychain access control")
            setObject(accessControl, forKey = kSecAttrAccessControl as NSString)
            CFRelease(accessControl)
        } else {
            setObject(kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly, forKey = platform.Security.kSecAttrAccessible as NSString)
        }
    }

    private fun checkStatus(status: OSStatus, operation: String) {
        check(status == errSecSuccess) { "Unable to $operation: OSStatus $status" }
    }
}

@OptIn(ExperimentalForeignApi::class)
private fun NSMutableDictionary.asCfDictionary(): CFDictionaryRef =
    this as CFDictionaryRef

@OptIn(ExperimentalForeignApi::class)
internal fun ByteArray.toNSData(): NSData =
    usePinned { pinned ->
        NSData.create(bytes = pinned.addressOf(0), length = size.convert())
    }

@OptIn(ExperimentalForeignApi::class)
internal fun NSData.toByteArray(): ByteArray {
    val bytes = ByteArray(length.toInt())
    if (bytes.isEmpty()) return bytes
    bytes.usePinned { pinned ->
        platform.posix.memcpy(pinned.addressOf(0), this.bytes, length)
    }
    return bytes
}
