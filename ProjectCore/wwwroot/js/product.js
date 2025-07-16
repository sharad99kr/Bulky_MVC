﻿
$(document).ready(function () {
    loadDataTable();
});


function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/product/getall' },
        "columns": [{ data: 'title', "width": "15%" },
                    { data: 'isbn', "width":"15%" },
                    { data: 'price', "width": "15%" },
                    { data: 'author', "width": "15%" },
                    { data: 'category', "width": "15%" },]
    });

}
