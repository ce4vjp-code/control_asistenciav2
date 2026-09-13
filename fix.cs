using System;
using System.IO;
using System.Text;

class Program {
    static void Main() {
        string[] files = { 
            @"AsistenciaDesktop_v2\Views\LoginWindow.xaml", 
            @"AsistenciaDesktop_v2\Views\AlumnosView.xaml", 
            @"AsistenciaDesktop_v2\Views\UsuariosView.xaml", 
            @"AsistenciaDesktop_v2\Views\MainShell.xaml" 
        };
        foreach (var f in files) {
            if (File.Exists(f)) {
                string content = File.ReadAllText(f, Encoding.UTF8);
                content = content.Replace("INICIAR SESIN", "INICIAR SESIÓN");
                content = content.Replace("INICIAR SESI?N", "INICIAR SESIÓN");
                content = content.Replace("Contrasea", "Contraseña");
                content = content.Replace("Contrase?a", "Contraseña");
                content = content.Replace("Gestin de Alumnos", "Gestión de Alumnos");
                content = content.Replace("Gesti?n de Alumnos", "Gestión de Alumnos");
                content = content.Replace("Gestión de Alumns", "Gestión de Alumnos");
                content = content.Replace("Gestión de Alumn?s", "Gestión de Alumnos");
                File.WriteAllText(f, content, Encoding.UTF8);
            }
        }
    }
}
