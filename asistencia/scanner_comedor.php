<?php 
require_once 'auth.php';
include 'header.php'; 
?>
<div class="row text-center mt-5">
    <div class="col-md-6 offset-md-3">
        <h2 class="mb-4">Escanear Asistencia COMEDOR</h2>
        <div id="statusAlert" class="alert d-none"></div>
        <div class="card shadow-sm p-4">
            <h5 class="card-title">Pase el código por el lector</h5>
            <form id="scanForm" onsubmit="return false;">
                <input type="text" id="scanInput" class="form-control text-center fs-4" autofocus autocomplete="off" placeholder="Esperando escaneo...">
            </form>
        </div>
    </div>
</div>

<script>
document.addEventListener('DOMContentLoaded', function() {
    const input = document.getElementById('scanInput');
    const alertBox = document.getElementById('statusAlert');
    
    document.addEventListener('click', () => { if (!input.disabled) input.focus(); });
    input.focus();
    
    document.getElementById('scanForm').addEventListener('submit', function(e) {
        e.preventDefault();
        const code = input.value.trim();
        if(!code) return;
        
        input.value = ''; 
        input.disabled = true; // Bloquear ingreso doble
        
        const csrfToken = '<?php echo csrf_token(); ?>';
        fetch('registrar_asistencia.php', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'tipo=comedor&numero_unico=' + encodeURIComponent(code) + '&csrf_token=' + encodeURIComponent(csrfToken)
        })
        .then(res => res.json())
        .then(data => {
            alertBox.className = 'alert mt-3 ' + (data.status === 'success' ? 'alert-success' : (data.status === 'warning' ? 'alert-warning' : 'alert-danger'));
            alertBox.innerText = data.message;
            alertBox.classList.remove('d-none');
            setTimeout(() => alertBox.classList.add('d-none'), 3000);
        })
        .catch(err => {
            alertBox.className = 'alert mt-3 alert-danger';
            alertBox.innerText = 'Error de conexión';
            alertBox.classList.remove('d-none');
        })
        .finally(() => {
            input.disabled = false;
            input.focus();
        });
    });
});
</script>
<?php include 'footer.php'; ?>
