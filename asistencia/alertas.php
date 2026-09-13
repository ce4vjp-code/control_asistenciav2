<?php 
$require_admin = true;
require_once 'auth.php';
require_once 'procesar_alertas.php'; // Ya hace el cálculo del día de hoy en $faltantes
include 'header.php'; 
?>
<div class="row mb-4">
    <div class="col-md-6"><h2>Alertas Comedor (Hoy)</h2></div>
    <div class="col-md-6 text-end">
        <button type="button" class="btn btn-success me-2" onclick="descargarPDF()">Descargar PDF</button>
        <form method="post" action="procesar_alertas.php" class="d-inline">
            <?php csrf_field(); ?>
            <input type="hidden" name="enviar_alertas" value="1">
            <button type="submit" class="btn btn-danger" onclick="return confirm('¿Desea enviar las notificaciones de alerta por correo ahora?');">Enviar Notificación Manualmente Ahora</button>
        </form>
    </div>
</div>

<?php if(isset($_GET['enviado'])): ?>
<div class="alert alert-success">Las notificaciones han sido enviadas exitosamente.</div>
<?php endif; ?>

<div class="row">
    <div class="col-12" id="tablaAlertas">
        <div id="tituloPdfAlertas" class="d-none text-center mb-4">
            <h2>Reporte de Alertas - Comedor</h2>
            <h4>Fecha: <?php echo date('d-m-Y'); ?></h4>
            <hr>
        </div>
        <?php if(!empty($faltantes)): ?>
        <div class="alert alert-danger" data-html2canvas-ignore="true">
            Se detectaron <?php echo count($faltantes); ?> alumno(s) que ingresaron al colegio hoy pero no registraron asistencia en el comedor.
        </div>
        <table class="table table-striped table-bordered">
            <thead class="table-dark"><tr><th>RUT/ID</th><th>Nombre</th><th>Curso</th></tr></thead>
            <tbody>
                <?php foreach($faltantes as $f): ?>
                <tr>
                    <td><?php echo htmlspecialchars($f['numero_unico']); ?></td>
                    <td><?php echo htmlspecialchars($f['nombre']); ?></td>
                    <td><?php echo htmlspecialchars($f['curso']); ?></td>
                </tr>
                <?php endforeach; ?>
            </tbody>
        </table>
        <?php else: ?>
        <div class="alert alert-success">Todos los alumnos que asistieron hoy al colegio han pasado por el comedor.</div>
        <?php endif; ?>
    </div>
</div>

<!-- Incluir html2pdf -->
<script src="https://cdnjs.cloudflare.com/ajax/libs/html2pdf.js/0.10.1/html2pdf.bundle.min.js"></script>
<script>
function descargarPDF() {
    const element = document.getElementById('tablaAlertas');
    const tituloPdf = document.getElementById('tituloPdfAlertas');
    
    // Mostrar el título sólo para el PDF
    tituloPdf.classList.remove('d-none');
    
    const opt = {
      margin:       0.5,
      filename:     'Alertas_Comedor_<?php echo date('Y-m-d'); ?>.pdf',
      image:        { type: 'jpeg', quality: 0.98 },
      html2canvas:  { scale: 2 },
      jsPDF:        { unit: 'in', format: 'letter', orientation: 'portrait' }
    };
    
    html2pdf().set(opt).from(element).save().then(() => {
        // Volver a ocultar el título en la web
        tituloPdf.classList.add('d-none');
    });
}
</script>
<?php include 'footer.php'; ?>
