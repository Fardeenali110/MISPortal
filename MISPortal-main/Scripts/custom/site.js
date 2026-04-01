// MIS Portal Custom JavaScript

$(document).ready(function () {
    console.log("MIS Portal initialized");

    // Auto-hide alert messages after 5 seconds
    setTimeout(function () {
        $('.alert').fadeOut('slow');
    }, 5000);

    // Enable tooltips
    $('[data-toggle="tooltip"]').tooltip();

    // Confirm delete actions
    $('.btn-delete').on('click', function (e) {
        if (!confirm('Are you sure you want to delete this item?')) {
            e.preventDefault();
            return false;
        }
    });

    // Form validation enhancement
    $('form').on('submit', function () {
        $(this).find(':submit').prop('disabled', true);
        $(this).find(':submit').html('<i class="fas fa-spinner fa-spin"></i> Processing...');
    });

    // Auto-format phone numbers
    $('.phone-mask').inputmask('(999) 999-9999');

    // Auto-format dates
    $('.date-picker').datepicker({
        format: 'yyyy-mm-dd',
        autoclose: true,
        todayHighlight: true
    });

    // Table row click for details
    $('.clickable-row').click(function () {
        window.location = $(this).data('href');
    });
});

// Global functions
function showLoading() {
    $('#loadingModal').modal('show');
}

function hideLoading() {
    $('#loadingModal').modal('hide');
}

function showSuccess(message) {
    toastr.success(message, 'Success');
}

function showError(message) {
    toastr.error(message, 'Error');
}

function showWarning(message) {
    toastr.warning(message, 'Warning');
}

// Ajax form submission
function submitFormAjax(formId, successCallback, errorCallback) {
    var form = $('#' + formId);
    var formData = new FormData(form[0]);

    $.ajax({
        url: form.attr('action'),
        type: form.attr('method'),
        data: formData,
        processData: false,
        contentType: false,
        beforeSend: function () {
            showLoading();
        },
        success: function (response) {
            hideLoading();
            if (response.success) {
                showSuccess(response.message);
                if (successCallback) successCallback(response);
            } else {
                showError(response.message);
                if (errorCallback) errorCallback(response);
            }
        },
        error: function (xhr, status, error) {
            hideLoading();
            showError('An error occurred: ' + error);
            if (errorCallback) errorCallback(xhr);
        }
    });
}

// Export to Excel
function exportToExcel(tableId, fileName) {
    var table = document.getElementById(tableId);
    var wb = XLSX.utils.table_to_book(table, { sheet: "Sheet1" });
    XLSX.writeFile(wb, fileName + '.xlsx');
}

// Print table
function printTable(tableId) {
    var printWindow = window.open('', '_blank');
    printWindow.document.write('<html><head><title>Print</title>');
    printWindow.document.write('<link rel="stylesheet" href="/Content/bootstrap.min.css">');
    printWindow.document.write('</head><body>');
    printWindow.document.write(document.getElementById(tableId).outerHTML);
    printWindow.document.write('</body></html>');
    printWindow.document.close();
    printWindow.focus();
    printWindow.print();
    printWindow.close();
}