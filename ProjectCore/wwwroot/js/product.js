﻿var dataTable;

$(document).ready(function () {
    loadDataTable();
});


function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/product/getall' },
        "columns": [{ data: 'title', "width": "25%" },
                    { data: 'isbn', "width":"15%" },
                    { data: 'price', "width": "10%" },
                    { data: 'author', "width": "15%" },
                    { data: 'category.name', "width": "10%" },
                    {
                        data: 'id',
                        "render": function (data) {
                            return `<div class="w-75 btn-group" role = "group">
                                <a href = "/admin/product/upsert?id=${data}" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i> EDIT</a >
                                <a onClick = "Delete( '/admin/product/delete/${data}')" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i> DELETE</a >
                                </div >`
                        },"width": "25%"
            }]
    });

}
//coulmns count and value must match with view or else datatables will not work

function Delete(url) {
    Swal.fire({
        title: "Are you sure?",
        text: "You won't be able to revert this!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    dataTable.ajax.reload();
                    toastr.success(data.message);
                }
            })
        }
    });
}