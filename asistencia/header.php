<?php
require_once __DIR__ . '/session_config.php';
$is_logged_in = isset($_SESSION['admin_logged_in']) && $_SESSION['admin_logged_in'] === true;
$user_rol = $_SESSION['user_rol'] ?? 'operador';
?>
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <title>Asistencia Escolar PHP</title>
    <!-- Bootstrap 5 -->
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
    <!-- Google Fonts: Inter -->
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600&display=swap" rel="stylesheet">
    <style>
        :root {
            /* Colores suaves y modernos */
            --bs-primary: #507d96;
            --bs-primary-rgb: 80, 125, 150;
            --bs-body-bg: #f5f8fa;
            --bs-body-color: #334155;
            --bs-font-sans-serif: 'Inter', sans-serif;
            --navbar-bg: #ffffff;
            --table-header-bg: #64748b;
        }

        body {
            font-family: var(--bs-font-sans-serif);
            background-color: var(--bs-body-bg);
            color: var(--bs-body-color);
            padding-top: 80px; /* Un poco más de espacio para la navbar moderna */
        }

        .hidden-input { position: absolute; left: -9999px; }

        /* Navbar Moderna (Fondo blanco y sombras suaves) */
        .navbar-custom {
            background-color: var(--navbar-bg);
            box-shadow: 0 4px 20px rgba(0,0,0,0.04);
            padding: 12px 0;
            border-bottom: 1px solid #e2e8f0;
        }
        .navbar-custom .navbar-brand {
            color: #0f172a;
            font-weight: 600;
            letter-spacing: -0.5px;
        }
        .navbar-custom .nav-link {
            color: #475569;
            font-weight: 500;
            transition: all 0.2s;
            margin: 0 4px;
            border-radius: 6px;
        }
        .navbar-custom .nav-link:hover {
            color: var(--bs-primary);
            background-color: #f1f5f9;
        }
        .navbar-toggler { border-color: #cbd5e1; }
        .navbar-toggler-icon { filter: invert(1); opacity: 0.6; }

        /* Tarjetas (Cards) */
        .card {
            border: 1px solid #e2e8f0;
            border-radius: 12px;
            box-shadow: 0 10px 25px rgba(0,0,0,0.03) !important;
            overflow: hidden;
        }
        .card-header {
            background-color: #f8fafc !important;
            color: #0f172a !important;
            border-bottom: 1px solid #e2e8f0;
            font-weight: 600;
            padding: 16px 20px;
        }
        .card-header.bg-primary {
            background-color: var(--bs-primary) !important;
            color: white !important;
        }

        /* Botones suaves */
        .btn {
            border-radius: 8px;
            font-weight: 500;
            padding: 8px 16px;
            transition: all 0.2s ease;
        }
        .btn-primary {
            background-color: var(--bs-primary);
            border-color: var(--bs-primary);
        }
        .btn-primary:hover {
            background-color: #3e6378;
            border-color: #3e6378;
            transform: translateY(-1px);
        }

        /* Tablas */
        .table {
            background-color: #ffffff;
            border-radius: 8px;
            overflow: hidden;
            border-collapse: separate;
            border-spacing: 0;
            border: 1px solid #e2e8f0;
        }
        .table th, .table td {
            vertical-align: middle;
            border-top: 1px solid #e2e8f0;
        }
        .table-dark th {
            background-color: var(--table-header-bg) !important;
            color: #f8fafc;
            font-weight: 500;
            border-bottom: 0;
            border-color: var(--table-header-bg) !important;
        }
        .table-striped>tbody>tr:nth-of-type(odd)>* {
            background-color: #f8fafc;
        }
        
        /* Alertas */
        .alert {
            border-radius: 10px;
            border: none;
            font-weight: 500;
        }
        .alert-success { background-color: #ecfdf5; color: #065f46; }
        .alert-danger { background-color: #fef2f2; color: #991b1b; }
        
        /* Títulos */
        h1, h2, h3, h4, h5 {
            font-weight: 600;
            color: #1e293b;
        }
    </style>
</head>
<body>
    <nav class="navbar navbar-expand-lg navbar-custom fixed-top">
        <div class="container-fluid">
            <a class="navbar-brand" href="index.php">Asistencia LICEOTPGGM</a>
            <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
                <span class="navbar-toggler-icon"></span>
            </button>
            <div class="collapse navbar-collapse" id="navbarNav">
                <ul class="navbar-nav me-auto">
                    <?php if ($is_logged_in): ?>
                    <li class="nav-item"><a class="nav-link" href="scanner_colegio.php">Scanner Colegio</a></li>
                    <li class="nav-item"><a class="nav-link" href="scanner_comedor.php">Scanner Comedor</a></li>
                    <?php if (($user_rol ?? '') === 'admin'): ?>
                    <li class="nav-item"><a class="nav-link" href="alumnos.php">Gestión Alumnos</a></li>
                    <li class="nav-item"><a class="nav-link" href="reportes.php">Reportes</a></li>
                    <li class="nav-item"><a class="nav-link" href="alertas.php">Alertas Comedor</a></li>
                    <li class="nav-item"><a class="nav-link" href="bitacora.php">Bitácora</a></li>
                    <?php endif; ?>
                    <?php endif; ?>
                </ul>
                <?php if ($is_logged_in): ?>
                <ul class="navbar-nav ms-auto align-items-center">
                    <li class="nav-item me-2">
                        <span class="badge <?php echo ($user_rol ?? '') === 'admin' ? 'bg-primary' : 'bg-secondary'; ?>">
                            <?php echo htmlspecialchars(strtoupper($user_rol ?? 'operador')); ?>
                        </span>
                    </li>
                    <li class="nav-item"><a class="nav-link" href="cambiar_clave.php">Cambiar Clave</a></li>
                    <li class="nav-item"><a class="nav-link text-danger" href="logout.php">Cerrar Sesión</a></li>
                </ul>
                <?php endif; ?>
            </div>
        </div>
    </nav>
    <div class="container mt-4">
