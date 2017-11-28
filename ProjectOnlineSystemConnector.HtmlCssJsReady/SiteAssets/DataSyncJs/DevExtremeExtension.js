function DevExtremeCustomStore(systemProxyUrl, controllerName, keyFieldName, queryParameters, onLoadComplete) {
    var self = this;
    self.ParentGrid = {};
    self.ArrCache = [];
    self.TotalCountCache = 0;
    self.UpdateCounter = 0;
    self.InsertCounter = 0;
    self.BufferUpdate = [];
    self.BufferDefferedsUpdate = {};
    self.BufferInsert = [];
    self.BufferDefferedsInsert = {};
    self.BufferRemove = [];
    self.BufferDefferedsRemove = {};
    self.BufferNewValues = [];

    self.CustomStore = new DevExpress.data.CustomStore({
        //    key: keyFieldName,
        load: function (loadOptions) {
            if (loadOptions != null) {
                var filterOptions =
                    loadOptions.filter ? JSON.stringify(loadOptions.filter) : ""; // Getting filter settings
                var sortOptions = loadOptions.sort ? JSON.stringify(loadOptions.sort) : ""; // Getting sort settings
                var requireTotalCount =
                    loadOptions.requireTotalCount; // You can check this parameter on the server side  
                // to ensure that a number of records (totalCount) is required
                var skip = loadOptions.skip; // A number of records that should be skipped 
                var take = loadOptions.take; // A number of records that should be taken
            }

            var deferred = $.Deferred();
            if (self.ArrCache == null || self.ArrCache.length === 0) {
                CommonAjaxRequest(systemProxyUrl + controllerName + "/GetListAsync",
                    queryParameters,
                    "GET",
                    null,
                    function (result) {
                        self.ArrCache = $.parseJSON(result.Data);
                        self.TotalCountCache = result.TotalCount;
                        deferred.resolve(self.ArrCache,
                            {
                                totalCount: self.TotalCountCache
                            });
                        if (onLoadComplete != null) {
                            onLoadComplete();
                        }
                    },
                    function (jqXhr, textStatus, error) {
                        CommonAjaxError(jqXhr, textStatus, error, deferred);
                    });
            } else {
                deferred.resolve(self.ArrCache,
                    {
                        totalCount: self.TotalCountCache
                    });
            }

            return deferred.promise();
        },

        byKey: function (key) {
            var objects = $.grep(self.ArrCache,
                function (el) {
                    return el.Id === key;
                });

            var deferred = $.Deferred();
            deferred.resolve(objects[0]);
            return deferred.promise();
        },

        update: function (values, newvalues) {
            for (var key in newvalues) {
                if (values.hasOwnProperty(key) && newvalues.hasOwnProperty(key)) {
                    values[key] = newvalues[key];
                }
            }
            var deferred = $.Deferred();

            self.BufferDefferedsUpdate[values[keyFieldName]] = deferred;
            self.BufferUpdate.push(values);

            var id = values[keyFieldName];
            self.BufferNewValues.push({ id, newvalues });


            if (self.BufferUpdate.length === self.UpdateCounter) {
                self.ParentGrid.beginCustomLoading();
                CommonAjaxRequest(systemProxyUrl + controllerName + "/UpdateListAsync",
                    queryParameters,
                    "POST",
                    { UpdateList: self.BufferUpdate },
                    function (result) {
                        var arr = $.parseJSON(result.Data);
                        $.each(arr,
                            function (index, object) {
                                var tempDeferred = self.BufferDefferedsUpdate[object[keyFieldName]];
                                tempDeferred.resolve(object[keyFieldName]);
                            });
                        self.BufferUpdate = [];
                        self.BufferNewValues = [];
                        self.BufferDefferedsUpdate = {};
                        self.ArrCache = [];
                        self.UpdateCounter = 0;
                        self.ParentGrid.endCustomLoading();
                    },
                    function (jqXhr, textStatus, error) {
                        self.ParentGrid.endCustomLoading();
                        self.BufferUpdate = [];
                        self.BufferNewValues = [];
                        self.BufferDefferedsUpdate = {};
                        self.UpdateCounter = 0;
                        CommonAjaxError(jqXhr, textStatus, error, deferred);
                    });
            }
            return deferred.promise();
        },

        insert: function (values) {
            var deferred = $.Deferred();
            var key = "__KEY__";
            self.BufferDefferedsInsert[values[key]] = deferred;
            self.BufferInsert.push(values);
            if (self.BufferInsert.length === self.InsertCounter) {
                self.ParentGrid.beginCustomLoading();
                CommonAjaxRequest(systemProxyUrl + controllerName + "/InsertListAsync",
                    queryParameters,
                    "POST",
                    { InsertList: self.BufferInsert, Marker: "Test" },
                    function (result) {
                        var arr = $.parseJSON(result.Data);
                        $.each(arr,
                            function (index, object) {
                                var tempDeferred = self.BufferDefferedsInsert[object[key]];
                                tempDeferred.resolve(object[key]);
                            });
                        self.BufferInsert = [];
                        self.BufferDefferedsInsert = {};
                        self.ArrCache = [];
                        self.InsertCounter = 0;
                        self.ParentGrid.endCustomLoading();
                    },
                    function (jqXhr, textStatus, error) {
                        self.ParentGrid.endCustomLoading();
                        self.BufferInsert = [];
                        self.BufferDefferedsInsert = {};
                        self.InsertCounter = 0;
                        CommonAjaxError(jqXhr, textStatus, error, deferred);
                    });
            }
            return deferred.promise();
        },

        remove: function (values) {
            self.ParentGrid.beginCustomLoading();
            var deferred = $.Deferred();
            self.BufferDefferedsRemove[values[keyFieldName]] = deferred;
            self.BufferRemove.push(values[keyFieldName]);

            if (self.BufferRemove.length === $(".dx-row-removed").length) {
                CommonAjaxRequest(systemProxyUrl + controllerName + "/RemoveListAsync",
                    queryParameters,
                    "POST",
                    { DeleteList: self.BufferRemove },
                    function (result) {
                        var arr = $.parseJSON(result.Data);
                        $.each(arr,
                            function (index, id) {
                                var tempDeferred = self.BufferDefferedsRemove[id];
                                tempDeferred.resolve(id);
                            });
                        self.BufferRemove = [];
                        self.BufferDefferedsRemove = {};
                        self.ArrCache = [];
                        self.ParentGrid.endCustomLoading();
                    },
                    function (jqXhr, textStatus, error) {
                        self.ParentGrid.endCustomLoading();
                        self.BufferRemove = [];
                        self.BufferDefferedsRemove = {};
                        CommonAjaxError(jqXhr, textStatus, error, deferred);
                    });
            }
            return deferred.promise();
        }
    });
}

function GetDataGridOptions(systemProxyUrl,
    controllerName,
    columns,
    keyFieldName,
    queryParameters,
    onLoadComplete,
    controlHeight,
    allowEditing,
    devExtremeCustomStore) {

    var dataGridOptions = {};

    if (devExtremeCustomStore == null && controllerName != null) {
        devExtremeCustomStore = new DevExtremeCustomStore(systemProxyUrl,
            controllerName,
            keyFieldName,
            queryParameters,
            onLoadComplete);
    }
    if (devExtremeCustomStore != null) {
        dataGridOptions.dataSource = {
            store: devExtremeCustomStore.CustomStore
        };
        dataGridOptions.CustomStore = devExtremeCustomStore;
    }
    dataGridOptions.columns = columns;

    if (allowEditing) {
        dataGridOptions.editing = {
            mode: "batch",
            allowUpdating: true,
            allowAdding: true,
            allowDeleting: true
        };
    }

    dataGridOptions.remoteOperations = false;
    dataGridOptions.allowColumnReordering = true;
    dataGridOptions.allowColumnResizing = true;

    dataGridOptions.height = controlHeight;
    dataGridOptions.pager = {
        showPageSizeSelector: true,
        allowedPageSizes: [10, 20, 50],
        showInfo: true
    };
    dataGridOptions.paging = {
        pageSize: 20
    };

    dataGridOptions.filterRow = {
        visible: true,
        applyFilter: "auto"
    };
    dataGridOptions.searchPanel = {
        visible: true,
        width: 240,
        placeholder: "Search..."
    };
    dataGridOptions.headerFilter = {
        visible: true
    };

    dataGridOptions.export = {
        enabled: true,
        fileName: controllerName,
        allowExportSelectedData: false
    };

    dataGridOptions.groupPanel = {
        visible: true
    };
    dataGridOptions.loadPanel = {
        enabled: true,
        text: 'Data is processing'
    };
    dataGridOptions.onRowValidating = function (data) {
        if (data.isValid)
            if (data.oldData == null) {
                dataGridOptions.CustomStore.InsertCounter++;
            } else {
                dataGridOptions.CustomStore.UpdateCounter++;
            }
    };
    dataGridOptions.onCellPrepared = function (e) {
        if (e.rowType === "data" && e.column.command === "edit") {
            var removed = e.row.removed,
                $links = e.cellElement.find(".dx-link");

            $links.text("");

            if (removed) {
                $links.filter(".dx-link-undelete").addClass("dx-icon-revert");
                $links.text("Undel");
            } else {
                $links.filter(".dx-link-delete").addClass("dx-icon-trash");
                $links.text("Del");
            }
        }
    };

    return dataGridOptions;
}