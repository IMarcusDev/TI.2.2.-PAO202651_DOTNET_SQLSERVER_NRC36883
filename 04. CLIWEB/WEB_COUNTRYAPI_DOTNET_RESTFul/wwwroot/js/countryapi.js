/*
 * CountryAPI · cliente web. Lo único que hace JavaScript (todo funciona también sin él):
 *  1. Indicador «Servidor disponible / no disponible»: consulta /estado cada 10 segundos.
 *  2. Filtros que se envían solos al cambiar (atributo data-autoenviar en el formulario).
 */
(function () {
    'use strict';

    var indicador = document.querySelector('[data-indicador]');
    if (indicador) {
        var texto = indicador.querySelector('.indicador__texto');
        var comprobar = function () {
            fetch(indicador.getAttribute('data-indicador'), { cache: 'no-store', credentials: 'same-origin' })
                .then(function (respuesta) { return respuesta.json(); })
                .then(function (estado) { pintar(estado.disponible === true); })
                .catch(function () { pintar(false); });
        };
        var pintar = function (disponible) {
            indicador.setAttribute('data-estado', disponible ? 'si' : 'no');
            texto.textContent = disponible ? 'Servidor disponible' : 'Servidor no disponible';
        };
        comprobar();
        setInterval(comprobar, 10000);
    }

    document.querySelectorAll('form[data-autoenviar]').forEach(function (formulario) {
        formulario.querySelectorAll('input[type=radio], select').forEach(function (control) {
            control.addEventListener('change', function () { formulario.submit(); });
        });
    });
})();
