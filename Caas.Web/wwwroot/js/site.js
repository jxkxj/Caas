/*exported loadingApp*/

var loadingApp = new Vue({
    el: '#loading',
    data: {
        loading: false
    }
})

$(document).on('ready', function () {
    $(".container").css('opacity', '1')
})