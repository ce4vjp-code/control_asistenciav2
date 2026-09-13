<?php
require_once __DIR__ . '/session_config.php';

if (!isset($_SESSION['admin_logged_in']) || $_SESSION['admin_logged_in'] !== true) {
    // Si la petición espera JSON
    $accept = $_SERVER['HTTP_ACCEPT'] ?? '';
    if (strpos($accept, 'application/json') !== false || basename($_SERVER['PHP_SELF']) === 'registrar_asistencia.php') {
        http_response_code(401);
        header('Content-Type: application/json');
        echo json_encode(["status" => "error", "message" => "Sesión expirada o no autenticada. Inicie sesión nuevamente."]);
        exit;
    }
    
    header("Location: index.php");
    exit;
}

// Verificación de Rol Administrativo (RBAC)
if (isset($require_admin) && $require_admin === true) {
    $rol = $_SESSION['user_rol'] ?? 'operador';
    if ($rol !== 'admin') {
        http_response_code(403);
        die("<h1>403 Acceso Denegado</h1><p>Su cuenta tiene rol de <strong>operador</strong> y no tiene permisos para acceder a esta área de administración.</p><p><a href='scanner_colegio.php'>Ir a Escáner Colegio</a> | <a href='logout.php'>Cerrar Sesión</a></p>");
    }
}
?>
