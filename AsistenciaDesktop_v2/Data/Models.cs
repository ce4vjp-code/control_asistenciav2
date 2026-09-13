namespace AsistenciaDesktop_v2.Data
{
    public class Alumno
    {
        public string NumeroUnico { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Curso { get; set; } = "";
        public string EmailApoderado { get; set; } = "";
    }

    public class Usuario
    {
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Role { get; set; } = "";
        public string Email { get; set; } = "";
    }

    public class RegistroAsistencia
    {
        public string AlumnoId { get; set; } = "";
        public string Fecha { get; set; } = ""; // Formato yyyy-MM-dd
        public string HoraIngreso { get; set; } = "";
        public string? HoraSalida { get; set; }
        public bool IsSynced { get; set; } = false;
    }

    public class RegistroComedor
    {
        public string AlumnoId { get; set; } = "";
        public string Fecha { get; set; } = ""; // Formato yyyy-MM-dd
        public string HoraIngreso { get; set; } = "";
        public bool IsSynced { get; set; } = false;
    }
}
