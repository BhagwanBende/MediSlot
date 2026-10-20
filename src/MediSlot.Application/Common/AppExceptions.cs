namespace MediSlot.Application.Common;

public abstract class AppException(string message) : Exception(message);

public class ConflictException(string message) : AppException(message);

public class UnauthorizedException(string message) : AppException(message);

public class NotFoundException(string message) : AppException(message);

public class BadRequestException(string message) : AppException(message);