namespace POSales
{
    /// <summary>
    /// Simple in-memory session holder for the currently logged-in user.
    /// Set on successful login and read by other forms (e.g., StockIn).
    /// </summary>
    public static class UserSession
    {
        public static string Username { get; set; } = string.Empty;
        public static string Name { get; set; } = string.Empty;
        public static string Role { get; set; } = string.Empty;
    }
}
