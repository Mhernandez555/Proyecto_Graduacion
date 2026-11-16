(function () {
  function normalizar(texto) {
    return texto.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
  }

  document.querySelectorAll('[data-selector-usuarios]').forEach(function (selector) {
    var busqueda = selector.querySelector('[data-selector-busqueda]');
    var boton = selector.querySelector('[data-selector-toggle]');
    var opciones = selector.querySelector('[data-selector-opciones]');
    var seleccionados = selector.querySelector('[data-selector-seleccionados]');
    var mensaje = selector.querySelector('[data-selector-mensaje]');
    var urlUsuarios = selector.dataset.usuariosUrl;
    var proyectoSelect = selector.dataset.proyectoSelectId
      ? document.getElementById(selector.dataset.proyectoSelectId)
      : null;
    var solicitud = 0;

    function controles() {
      return Array.prototype.slice.call(opciones.querySelectorAll('[data-selector-checkbox]'));
    }

    function abrir(estado) {
      opciones.hidden = !estado;
      busqueda.setAttribute('aria-expanded', estado ? 'true' : 'false');
      boton.setAttribute('aria-expanded', estado ? 'true' : 'false');
    }

    function actualizar() {
      var termino = normalizar(busqueda.value.trim());
      var visibles = 0;
      controles().forEach(function (checkbox) {
        var fila = checkbox.closest('[data-selector-opcion]');
        var coincide = normalizar(fila.textContent).indexOf(termino) !== -1;
        fila.hidden = !coincide;
        if (coincide) visibles++;
      });

      seleccionados.replaceChildren();
      controles().filter(function (checkbox) { return checkbox.checked; }).forEach(function (checkbox) {
        var chip = document.createElement('span');
        chip.className = 'selector-usuarios-chip';
        var texto = document.createElement('span');
        texto.textContent = checkbox.parentElement.querySelector('span').textContent;
        var quitar = document.createElement('button');
        quitar.type = 'button';
        quitar.textContent = '×';
        quitar.setAttribute('aria-label', 'Quitar ' + texto.textContent);
        quitar.dataset.quitarUsuario = checkbox.value;
        chip.appendChild(texto);
        chip.appendChild(quitar);
        seleccionados.appendChild(chip);
      });

      if (controles().length === 0)
        mensaje.textContent = selector.dataset.mensajeSinOpciones || 'No hay usuarios disponibles.';
      else if (visibles === 0)
        mensaje.textContent = 'No se encontraron usuarios con ese nombre.';
      else
        mensaje.textContent = '';
    }

    function reemplazarOpciones(usuarios, idsSeleccionados) {
      opciones.replaceChildren();
      usuarios.forEach(function (usuario) {
        var fila = document.createElement('label');
        fila.className = 'selector-usuarios-opcion';
        fila.dataset.selectorOpcion = '';
        var checkbox = document.createElement('input');
        checkbox.type = 'checkbox';
        checkbox.name = selector.dataset.inputName;
        checkbox.value = usuario.id;
        checkbox.checked = idsSeleccionados.has(String(usuario.id));
        checkbox.dataset.selectorCheckbox = '';
        var texto = document.createElement('span');
        texto.textContent = usuario.texto;
        fila.appendChild(checkbox);
        fila.appendChild(texto);
        opciones.appendChild(fila);
      });
    }

    function cambiarProyecto() {
      var idSolicitud = ++solicitud;
      var idsSeleccionados = new Set(controles().filter(function (checkbox) {
        return checkbox.checked;
      }).map(function (checkbox) { return checkbox.value; }));
      busqueda.value = '';
      abrir(false);

      if (!proyectoSelect.value || proyectoSelect.value === '0') {
        reemplazarOpciones([], new Set());
        busqueda.disabled = true;
        boton.disabled = true;
        mensaje.textContent = 'Seleccione un proyecto para consultar sus participantes.';
        return;
      }

      reemplazarOpciones([], new Set());
      busqueda.disabled = true;
      boton.disabled = true;
      mensaje.textContent = 'Cargando participantes del proyecto...';
      var separador = urlUsuarios.indexOf('?') === -1 ? '?' : '&';
      fetch(urlUsuarios + separador + 'idProyecto=' + encodeURIComponent(proyectoSelect.value), {
        headers: { Accept: 'application/json' }
      }).then(function (respuesta) {
        if (!respuesta.ok) throw new Error('No fue posible cargar los participantes del proyecto.');
        return respuesta.json();
      }).then(function (usuarios) {
        if (idSolicitud !== solicitud) return;
        if (!Array.isArray(usuarios)) throw new Error('La respuesta de participantes no es válida.');
        reemplazarOpciones(usuarios, idsSeleccionados);
        actualizar();
        busqueda.disabled = usuarios.length === 0;
        boton.disabled = usuarios.length === 0;
        if (usuarios.length === 0)
          mensaje.textContent = 'No hay participantes asignados a este proyecto.';
      }).catch(function (error) {
        if (idSolicitud !== solicitud) return;
        console.error(error);
        mensaje.textContent = 'No se pudieron cargar los participantes. Cambie de proyecto e inténtelo de nuevo.';
      });
    }

    busqueda.disabled = controles().length === 0;
    boton.disabled = controles().length === 0;
    actualizar();
    busqueda.addEventListener('focus', function () {
      if (!busqueda.disabled) abrir(true);
    });
    busqueda.addEventListener('input', function () {
      actualizar();
      abrir(true);
    });
    busqueda.addEventListener('keydown', function (evento) {
      if (evento.key === 'Escape') {
        abrir(false);
      } else if (evento.key === 'Enter') {
        evento.preventDefault();
        var primera = controles().find(function (checkbox) {
          return !checkbox.checked && !checkbox.closest('[data-selector-opcion]').hidden;
        });
        if (primera) {
          primera.checked = true;
          actualizar();
          abrir(true);
        }
      }
    });
    boton.addEventListener('click', function () {
      if (!boton.disabled) {
        abrir(opciones.hidden);
        busqueda.focus();
      }
    });
    opciones.addEventListener('change', function (evento) {
      if (evento.target.matches('[data-selector-checkbox]')) actualizar();
    });
    seleccionados.addEventListener('click', function (evento) {
      var quitar = evento.target.closest('[data-quitar-usuario]');
      if (!quitar) return;
      var checkbox = controles().find(function (item) { return item.value === quitar.dataset.quitarUsuario; });
      if (checkbox) {
        checkbox.checked = false;
        actualizar();
      }
    });
    document.addEventListener('click', function (evento) {
      if (!selector.contains(evento.target)) abrir(false);
    });

    if (urlUsuarios && proyectoSelect) {
      if (!proyectoSelect.value || proyectoSelect.value === '0') {
        busqueda.disabled = true;
        boton.disabled = true;
      }
      proyectoSelect.addEventListener('change', cambiarProyecto);
    }
  });
})();
