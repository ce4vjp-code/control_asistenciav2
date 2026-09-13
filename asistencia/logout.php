<?php
require_once __DIR__ . '/session_config.php';
require_once 'db.php';
require_once 'audit.php';

// Registrar salida en la bitácora
if (isset($_SESSION['admin_username'])) {
    registrar_bitacora($pdo, 'LOGOUT', "Usuario: {$_SESSION['admin_username']}");
}

// Limpiar todas las variables de sesión
$_SESSION = [];

// Si se usan cookies de sesión, destruirla
if (ini_get("session.use_cookies")) {
    $params = session_get_cookie_params();
    setcookie(
        session_name(),
        '',
        time() - 42000,
        $params["path"],
        $params["domain"],
        $params["secure"],
        $params["httponly"]
    );
}

// Destruir la sesión en el servidor
session_destroy();

header("Location: index.php");
exit;
?>
