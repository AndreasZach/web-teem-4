﻿window.onload = function () {
    this.GetServiceCodes();
}
function GetServiceCodes() {
    var apikey = '87a7d90ab3e0cb85d3a18ab17afd31d6';
    var url = 'https://cors-anywhere.herokuapp.com/https://atapi2.postnord.com/rest/shipment/v3/edi/servicecodes?apikey=';
    var request = new XMLHttpRequest();
    request.open('GET', url + apikey);
    request.onload = function () {
        var data = JSON.parse(request.responseText);
        console.log(data);
    };
    request.send();
}

function renderHTML(data) {


}