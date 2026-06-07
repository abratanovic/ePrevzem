package si.mentis.eprevzemmobile.feature.accountpicker

import si.mentis.eprevzemmobile.domain.AppUser
import si.mentis.eprevzemmobile.domain.EmployeeRole
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNull

class AccountPickerMapperTest {

    private val citizen = AppUser.RegularUser(
        id = "u-1", fullName = "Marko Horvat", email = "m@x.si", phone = "+386 41 000 001",
    )
    private val employee = AppUser.Employee(
        id = "u-2", fullName = "Ana Novak", email = "a@x.si", phone = "+386 41 000 002",
        status = "Aktiven", validUntil = "31. dec 2026",
        organizationId = "org-1", organizationName = "MNZ", organizationType = "Državni organ",
        organizationLocation = "Štefanova 2", roles = listOf(EmployeeRole.Operator),
    )

    @Test
    fun maps_citizen_to_citizen_row_without_organization() {
        val rows = listOf(citizen).toAccountRows()
        assertEquals(1, rows.size)
        assertEquals("u-1", rows[0].id)
        assertEquals("Marko Horvat", rows[0].fullName)
        assertEquals(AccountType.Citizen, rows[0].type)
        assertNull(rows[0].organizationName)
    }

    @Test
    fun maps_employee_to_employee_row_with_organization() {
        val rows = listOf(employee).toAccountRows()
        assertEquals(AccountType.Employee, rows[0].type)
        assertEquals("MNZ", rows[0].organizationName)
    }

    @Test
    fun preserves_order() {
        val rows = listOf(citizen, employee).toAccountRows()
        assertEquals(listOf("u-1", "u-2"), rows.map { it.id })
    }
}
