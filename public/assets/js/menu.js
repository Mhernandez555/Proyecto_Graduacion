// Abre y cierra el menú lateral en pantallas pequeñas.
(function () {
  var cuerpo = document.body;
  var boton = document.getElementById('menu-toggle');
  var fondo = document.getElementById('nav-overlay');
  if (!boton) return;

  function abrir(estado) {
    cuerpo.classList.toggle('menu-abierto', estado);
    boton.setAttribute('aria-expanded', estado ? 'true' : 'false');
  }

  boton.addEventListener('click', function () { abrir(!cuerpo.classList.contains('menu-abierto')); });
  if (fondo) fondo.addEventListener('click', function () { abrir(false); });
  document.addEventListener('keydown', function (e) { if (e.key === 'Escape') abrir(false); });
  document.querySelectorAll('.sidebar a').forEach(function (a) {
    a.addEventListener('click', function () { abrir(false); });
  });
  window.matchMedia('(min-width: 901px)').addEventListener('change', function (e) { if (e.matches) abrir(false); });
})();
