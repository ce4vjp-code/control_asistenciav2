import os

files = [
    r'AsistenciaDesktop_v2\Views\LoginWindow.xaml',
    r'AsistenciaDesktop_v2\Views\AlumnosView.xaml',
    r'AsistenciaDesktop_v2\Views\UsuariosView.xaml',
    r'AsistenciaDesktop_v2\Views\MainShell.xaml'
]

for f in files:
    if os.path.exists(f):
        with open(f, 'rb') as file:
            content = file.read()
        
        # U+FFFD is EF BF BD in UTF-8
        content = content.replace(b'INICIAR SESI\xef\xbf\xbdN', 'INICIAR SESIÓN'.encode('utf-8'))
        content = content.replace(b'Contrase\xef\xbf\xbda', 'Contraseña'.encode('utf-8'))
        content = content.replace(b'Gesti\xef\xbf\xbdn de', 'Gestión de'.encode('utf-8'))
        content = content.replace(b'Men\xef\xbf\xbd principal', 'Menú principal'.encode('utf-8'))
        
        with open(f, 'wb') as file:
            file.write(content)
