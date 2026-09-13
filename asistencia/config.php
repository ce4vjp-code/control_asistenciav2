<?php
// Prevenir acceso directo al archivo de configuración
if (basename($_SERVER['PHP_SELF']) === 'config.php') {
    http_response_code(403);
    exit('Acceso denegado');
}

return [
    'db' => [
        'host' => 'localhost',
        'dbname' => 'liceotpg_asistencia',
        'username' => 'liceotpg_asistencia',
        'password' => 'Dark19$$78',
        'charset' => 'utf8mb4'
    ],
    'mail' => [
        'host' => 'mail.liceotpggm.cl',
        'username' => 'no-reply@liceotpggm.cl',
        'password' => '=6s7dZE$Vr',
        'port' => 465,
        'from_name' => 'Liceo TP Gonzalo Guglielmi M.',
        'admin_alert' => 'inspector@liceotpggm.cl'
    ]
];
