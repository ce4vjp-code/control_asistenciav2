import os

f = r'AsistenciaDesktop_v2\Views\UsuariosView.xaml.cs'
with open(f, 'rb') as file:
    content = file.read()

content = content.replace(b'\xef\xbf\xbdxito', b'\xc3\x89xito')
content = content.replace(b'configuraci\xef\xbf\xbdn', b'configuraci\xc3\xb3n')
content = content.replace(b'Acci\xef\xbf\xbdn denegada', b'Acci\xc3\xb3n denegada')
content = content.replace(b'eliminaci\xef\xbf\xbdn', b'eliminaci\xc3\xb3n')
content = content.replace(b'\xef\xbf\xbdEliminar', b'\xc2\xbfEliminar')

with open(f, 'wb') as file:
    file.write(content)
