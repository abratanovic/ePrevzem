namespace ePrevzem.Application.Organizations;

public sealed class EmployeeNotFoundException(Guid id) : Exception($"Employee {id} not found.");
public sealed class EmployeeForbiddenException() : Exception("Employee belongs to a different organisation.");
