(function () {
  var clavePreferencia = 'controlex-tema';
  var raiz = document.documentElement;
  raiz.dataset.tema = localStorage.getItem(clavePreferencia) === 'nocturno' ? 'nocturno' : 'diurno';

  function configurarBoton() {
    var boton = document.getElementById('tema-toggle');
    if (!boton) return;

    function aplicarTema(tema) {
      var esNocturno = tema === 'nocturno';
      raiz.dataset.tema = esNocturno ? 'nocturno' : 'diurno';
      boton.textContent = esNocturno ? 'Claro' : 'Oscuro';
      boton.setAttribute('aria-label', esNocturno ? 'Activar modo diurno' : 'Activar modo nocturno');
      boton.setAttribute('aria-pressed', esNocturno ? 'true' : 'false');
    }

    aplicarTema(raiz.dataset.tema);
    boton.addEventListener('click', function () {
      var tema = raiz.dataset.tema === 'nocturno' ? 'diurno' : 'nocturno';
      localStorage.setItem(clavePreferencia, tema);
      aplicarTema(tema);
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', configurarBoton);
  } else {
    configurarBoton();
  }
})();
