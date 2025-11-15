namespace CrudCloudDb.Application.DTOs.Common
{
    public class ApiResponse<T>
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public static ApiResponse<T> Success(T data, string message = "Operación exitosa.")
        {
            return new ApiResponse<T> { Succeeded = true, Data = data, Message = message };
        }

        public static ApiResponse<T> Fail(string errorMessage, List<string>? errors = null)
        {
            return new ApiResponse<T> { Succeeded = false, Message = errorMessage, Errors = errors };
        }
    }
}