<?php
// Prevenir acceso directo
if (basename($_SERVER['PHP_SELF']) === 'session_config.php') {
    http_response_code(403);
    exit('Acceso denegado');
}

// Configuración de cookies seguras de sesión antes de iniciar la sesión
if (session_status() === PHP_SESSION_NONE) {
    $is_https = (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off') || (isset($_SERVER['SERVER_PORT']) && $_SERVER['SERVER_PORT'] == 443);
    
    // Parámetros compatibles para mitigar XSS y CSRF
    if (defined('PHP_VERSION_ID') && PHP_VERSION_ID >= 70300) {
        session_set_cookie_params([
            'lifetime' => 0,
            'path' => '/',
            'secure' => $is_https,
            'httponly' => true,
            'samesite' => 'Lax'
        ]);
    } else {
        session_set_cookie_params(0, '/', '', $is_https, true);
    }
    
    @session_start();
}

// Control de expiración de sesión por inactividad (30 minutos = 1800 segundos)
if (isset($_SESSION['admin_logged_in']) && $_SESSION['admin_logged_in'] === true) {
    $tiempo_max_inactividad = 1800;
    if (isset($_SESSION['last_activity']) && (time() - $_SESSION['last_activity'] > $tiempo_max_inactividad)) {
        $_SESSION = [];
        if (ini_get("session.use_cookies")) {
            $params = session_get_cookie_params();
            setcookie(session_name(), '', time() - 42000, $params["path"], $params["domain"], $params["secure"], $params["httponly"]);
        }
        @session_destroy();
        if (basename($_SERVER['PHP_SELF']) !== 'index.php') {
            header("Location: index.php?timeout=1");
            exit;
        }
    }
    $_SESSION['last_activity'] = time();
}

/**
 * Genera o recupera el token CSRF actual para el formulario
 */
function csrf_token() {
    if (empty($_SESSION['csrf_token'])) {
        $_SESSION['csrf_token'] = bin2hex(random_bytes(32));
    }
    return $_SESSION['csrf_token'];
}

/**
 * Imprime un campo input oculto con el token CSRF
 */
function csrf_field() {
    echo '<input type="hidden" name="csrf_token" value="' . htmlspecialchars(csrf_token(), ENT_QUOTES, 'UTF-8') . '">';
}

/**
 * Valida un token CSRF enviado por POST
 */
function verify_csrf_token($token) {
    if (empty($_SESSION['csrf_token']) || empty($token)) {
        return false;
    }
    return hash_equals($_SESSION['csrf_token'], $token);
}
