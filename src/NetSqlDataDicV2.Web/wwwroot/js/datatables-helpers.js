// DataTables initialization helpers
const DataTableHelpers = {
    // Standard configuration for server-side processing
    getServerSideConfig: function(ajaxUrl, columns) {
        return {
            processing: true,
            serverSide: true,
            ajax: {
                url: ajaxUrl,
                type: 'POST',
                contentType: 'application/json',
                data: function(d) {
                    return JSON.stringify({
                        page: (d.start / d.length) + 1,
                        pageSize: d.length,
                        sortField: d.columns[d.order[0]?.column]?.data,
                        sortDirection: d.order[0]?.dir || 'asc',
                        searchTerm: d.search?.value
                    });
                },
                dataFilter: function(data) {
                    var json = JSON.parse(data);
                    return JSON.stringify({
                        draw: json.draw,
                        recordsTotal: json.total,
                        recordsFiltered: json.total,
                        data: json.data
                    });
                }
            },
            columns: columns,
            pageLength: 50,
            lengthMenu: [[25, 50, 100, 200], [25, 50, 100, 200]],
            order: [[0, 'asc']],
            responsive: true
        };
    },

    // Show loading spinner
    showProgress: function(show) {
        if (show) {
            $('body').append('<div class="dt-loading-overlay"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div></div>');
        } else {
            $('.dt-loading-overlay').remove();
        }
    },

    // Alert dialog (Bootstrap modal)
    alert: function(message, title = 'Alert') {
        return new Promise((resolve) => {
            const modal = $(`
                <div class="modal fade" tabindex="-1">
                    <div class="modal-dialog">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title">${title}</h5>
                                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                            </div>
                            <div class="modal-body"><p>${message}</p></div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-primary" data-bs-dismiss="modal">OK</button>
                            </div>
                        </div>
                    </div>
                </div>
            `).appendTo('body');

            modal.on('hidden.bs.modal', function() {
                modal.remove();
                resolve();
            });

            new bootstrap.Modal(modal[0]).show();
        });
    },

    // Confirm dialog (Bootstrap modal)
    confirm: function(message, title = 'Confirm') {
        return new Promise((resolve) => {
            const modal = $(`
                <div class="modal fade" tabindex="-1">
                    <div class="modal-dialog">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title">${title}</h5>
                                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                            </div>
                            <div class="modal-body"><p>${message}</p></div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                                <button type="button" class="btn btn-danger btn-confirm">Confirm</button>
                            </div>
                        </div>
                    </div>
                </div>
            `).appendTo('body');

            modal.find('.btn-confirm').on('click', function() {
                bootstrap.Modal.getInstance(modal[0]).hide();
                resolve(true);
            });

            modal.on('hidden.bs.modal', function() {
                modal.remove();
                resolve(false);
            });

            new bootstrap.Modal(modal[0]).show();
        });
    }
};
