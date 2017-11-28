$(document).ready(function () {
    debugger;
    InitApplication();
    window.LoadPanelShow();
    GetSyncSettings(GetSyncSettingsSuccess);
});

function OnGetLogMessage(message) {
}

var LoadedTables = {
    SyncSystem: true,
    SyncSystemSetting: true,
    SyncSystemSettingMapping: true,
    SyncSystemType: true,
    SyncSystemFieldMapping: true,
    vProjectServerSystemLink: true,
};

function GetSyncSettingsSuccess() {
    SetUpSignalRHub(OnGetLogMessage, null);
    LoadPanelHide();

    var tabs = $("#tabs").tabs();
    tabs.on("click", function (evt) {
        var a = $(evt.target);
        if (a.attr("id") == null || a.attr("id").indexOf("ui-id") < 0) {
            a = $(evt.target).parent();
        }
        if (a.attr("id") == null || a.attr("id").indexOf("ui-id") < 0) {
            return;
        }
        var href = a.attr("href");
        var gridInstance = $(href).find("div[id^='gridContainer__']").dxDataGrid("instance");
        if (gridInstance != null) {
            gridInstance.refresh();
        }
    });
    var gridHeight = 400;

    $("#ButtonMergeDbToEpm").dxButton({
        text: "Merge DB To EPM",
        onClick: ButtonMergeDbToEpmClick
    }).dxButton("instance");

    //LinkedItemsTable.BuildGrid();

    //var syncSystemDataSource = new DevExpress.data.CustomStore({
    //    load: function (loadOptions) {
    //        var d = $.Deferred();
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemCRUD/ShowSyncSystem",
    //            method: "GET",
    //            dataType: "json",
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (result) {
    //            d.resolve($.parseJSON(result.Data), {
    //                totalCount: result.Data.lenght
    //            });
    //        }).fail(d.reject);
    //        return d.promise();
    //    },
    //    update: function (values, newvalues) {
    //        var deferred = $.Deferred();
    //        for (var key in newvalues) {
    //            if (values.hasOwnProperty(key) && newvalues.hasOwnProperty(key)) {
    //                values[key] = newvalues[key];
    //            }
    //        }
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemCRUD/UpdateSyncSystem",
    //            method: "POST",
    //            data: values,
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (data) {
    //            deferred.resolve(data.key);
    //        });
    //        return deferred.promise();
    //    },
    //    insert: function (values) {
    //        var deferred = $.Deferred();
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemCRUD/InsertSyncSystem",
    //            method: "POST",
    //            data: values,
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (data) {
    //            deferred.resolve(data.key);
    //        });
    //        return deferred.promise();
    //    },
    //    remove: function (key) {
    //        var deferred = $.Deferred();
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemCRUD/RemoveSyncSystem",
    //            method: "POST",
    //            data: key,
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (data) {
    //            deferred.resolve(data.key);
    //        });
    //        return deferred.promise();
    //    },
    //    byKey: function (key) {
    //        var d = new $.Deferred();
    //        $.get('http://data.example.com/products?id=' + key)
    //            .done(function (result) {
    //                d.resolve(result[i]);
    //            });
    //        return d.promise();
    //    }
    //});

    //var syncSystemSettingDataSource = new DevExpress.data.CustomStore({
    //    load: function (loadOptions) {
    //        var d = $.Deferred();
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemSettingCRUD/ShowSyncSystemSetting",
    //            method: "GET",
    //            dataType: "json",
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (result) {
    //            d.resolve($.parseJSON(result.Data), {
    //                totalCount: result.Data.lenght
    //            });
    //        }).fail(d.reject);
    //        return d.promise();
    //    },
    //    update: function (values, newvalues) {
    //        var deferred = $.Deferred();
    //        for (var key in newvalues) {
    //            if (values.hasOwnProperty(key) && newvalues.hasOwnProperty(key)) {
    //                values[key] = newvalues[key];
    //            }
    //        }
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemSettingCRUD/UpdateSyncSystemSetting",
    //            method: "POST",
    //            data: values,
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (data) {
    //            deferred.resolve(data.key);
    //        });
    //        return deferred.promise();
    //    },
    //    insert: function (values) {
    //        var deferred = $.Deferred();
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemSettingCRUD/InsertSyncSystemSetting",
    //            method: "POST",
    //            data: values,
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (data) {
    //            deferred.resolve(data.key);
    //        });
    //        return deferred.promise();
    //    },
    //    remove: function (key) {
    //        var deferred = $.Deferred();
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemSettingCRUD/RemoveSyncSystemSetting",
    //            method: "POST",
    //            data: key,
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (data) {
    //            deferred.resolve(data.key);
    //        });
    //        return deferred.promise();
    //    },
    //    byKey: function (key) {
    //        var d = new $.Deferred();
    //        $.get('http://data.example.com/products?id=' + key)
    //            .done(function (result) {
    //                d.resolve(result[i]);
    //            });
    //        return d.promise();
    //    }
    //});

    //var syncSystemSettingMappingDataSource = new DevExpress.data.CustomStore({
    //    load: function (loadOptions) {
    //        var d = $.Deferred();
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemSettingCRUD/ShowSyncSystemSettingMappings",
    //            method: "GET",
    //            dataType: "json",
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (result) {
    //            d.resolve($.parseJSON(result.Data), {
    //                totalCount: result.Data.lenght
    //            });
    //        }).fail(d.reject);
    //        return d.promise();
    //    },
    //    byKey: function (key) {
    //        var d = new $.Deferred();
    //        $.get('http://data.example.com/products?id=' + key)
    //            .done(function (result) {
    //                d.resolve(result[i]);
    //            });
    //        return d.promise();
    //    }
    //});

    //var syncSystemTypeDataSource = {};
    //syncSystemTypeDataSource["CustomStore"] = new DevExpress.data.CustomStore({
    //    load: function (loadOptions) {
    //        var d = $.Deferred();
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemTypeCRUD/GetListAsync",
    //            method: "GET",
    //            dataType: "json",
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (result) {
    //            d.resolve($.parseJSON(result.Data), {
    //                totalCount: result.Data.lenght
    //            });
    //        }).fail(d.reject);
    //        return d.promise();
    //    },
    //    byKey: function (key) {
    //        var d = new $.Deferred();
    //        return d.promise();
    //    },
    //    update: function (values, newvalues) {
    //        var deferred = $.Deferred();
    //        for (var key in newvalues) {
    //            if (values.hasOwnProperty(key) && newvalues.hasOwnProperty(key)) {
    //                values[key] = newvalues[key];
    //            }
    //        }
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemTypeCRUD/UpdateSyncSystemType",
    //            method: "POST",
    //            data: values,
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (data) {
    //            deferred.resolve(data.key);
    //        });
    //        return deferred.promise();
    //    },
    //    insert: function (values) {
    //        var deferred = $.Deferred();
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemTypeCRUD/InsertListAsync",
    //            method: "POST",
    //            data: values,
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (data) {
    //            deferred.resolve(data.key);
    //        });
    //        return deferred.promise();
    //    },
    //    remove: function (key) {
    //        var deferred = $.Deferred();
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemTypeCRUD/RemoveSyncSystemType",
    //            method: "POST",
    //            data: key,
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (data) {
    //            deferred.resolve(data.key);
    //        });
    //        return deferred.promise();
    //    }
    //});

    //var syncSystemFieldMappingDataSource = new DevExpress.data.CustomStore({
    //    load: function (loadOptions) {
    //        var d = $.Deferred();
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemFieldMappingCRUD/ShowSyncSystemFieldMapping",
    //            method: "GET",
    //            dataType: "json",
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (result) {
    //            d.resolve($.parseJSON(result.Data), {
    //                totalCount: result.Data.lenght
    //            });
    //        }).fail(d.reject);
    //        return d.promise();
    //    },
    //    byKey: function (key) {
    //        var d = new $.Deferred();
    //        return d.promise();
    //    },
    //    update: function (values, newvalues) {
    //        var deferred = $.Deferred();
    //        for (var key in newvalues) {
    //            if (values.hasOwnProperty(key) && newvalues.hasOwnProperty(key)) {
    //                values[key] = newvalues[key];
    //            }
    //        }
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemFieldMappingCRUD/UpdateSyncSystemFieldMapping",
    //            method: "POST",
    //            data: values,
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (data) {
    //            deferred.resolve(data.key);
    //        });
    //        return deferred.promise();
    //    },
    //    insert: function (values) {
    //        var deferred = $.Deferred();
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemFieldMappingCRUD/InsertSyncSystemFieldMapping",
    //            method: "POST",
    //            data: values,
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (data) {
    //            deferred.resolve(data.key);
    //        });
    //        return deferred.promise();
    //    },
    //    remove: function (key) {
    //        var deferred = $.Deferred();
    //        $.ajax({
    //            url: systemProxyUrl + "SyncSystemFieldMappingCRUD/RemoveSyncSystemFieldMapping",
    //            method: "POST",
    //            data: key,
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (data) {
    //            deferred.resolve(data.key);
    //        });
    //        return deferred.promise();
    //    }
    //});
    //var vProjectServerSystemLinkDataSource = new DevExpress.data.CustomStore({
    //    load: function (loadOptions) {
    //        var d = $.Deferred();
    //        var x = currentProjectUid;
    //        $.ajax({
    //            url: systemProxyUrl + "Common/GetProjectServerSystemLinkTable" + "?projUid=" + currentProjectUid,
    //            method: "GET",
    //            dataType: "json",
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (result) {
    //            d.resolve($.parseJSON(result.Data), {
    //                totalCount: result.Data.lenght
    //            });
    //        }).fail(d.reject);
    //        return d.promise();
    //    },
    //    byKey: function (key) {
    //        var d = new $.Deferred();
    //        $.get('http://data.example.com/products?id=' + key)
    //            .done(function (result) {
    //                d.resolve(result[i]);
    //            });
    //        return d.promise();
    //    }
    //});
    //var globalProjectsDataSource = new DevExpress.data.CustomStore({
    //    load: function (loadOptions) {
    //        var d = $.Deferred();
    //        //var x = currentProjectUid;
    //        $.ajax({
    //            url: systemProxyUrl + "Common/GetEpmProjects",
    //            method: "GET",
    //            dataType: "json",
    //            success: function (response) { },
    //            error: function (jqXhr, textStatus, error) { }
    //        }).done(function (result) {
    //            d.resolve($.parseJSON(result.Data), {
    //                totalCount: result.Data.lenght
    //            });
    //            linkedItemTableLoaded = true;
    //            if (accuracyTableLoaded) {
    //                LoadPanelHide();
    //            }
    //        }).fail(d.reject);
    //        return d.promise();
    //    },
    //    byKey: function (key) {
    //        var d = new $.Deferred();
    //        $.get('http://data.example.com/products?id=' + key)
    //            .done(function (result) {
    //                d.resolve(result[i]);
    //            });
    //        return d.promise();
    //    }
    //});

    var syncSystemTypeDataSource = new DevExtremeCustomStore(systemProxyUrl, "SyncSystemTypeCRUD", "SystemTypeId", null, null);
    syncSystemTypeDataSource.CustomStore.load();

    var syncSystemDataSource = new DevExtremeCustomStore(systemProxyUrl, "SyncSystemCRUD", "SystemId", null, null);
    syncSystemDataSource.CustomStore.load();

    var syncSystemSettingDataSource = new DevExtremeCustomStore(systemProxyUrl, "SyncSystemSettingCRUD", "SettingId", null, null);
    syncSystemSettingDataSource.CustomStore.load();

    var syncSystemColumns = [
        {
            dataField: "SystemId",
            caption: "System Id",
            allowEditing: false,
            width: 50
        },
        {
            dataField: "SystemTypeId",
            caption: "System Type Name",
            lookup: {
                dataSource: syncSystemTypeDataSource.CustomStore,
                valueExpr: 'SystemTypeId',
                displayExpr: 'SystemTypeName'
            },
            width: 100
        },
        {
            dataField: "SystemName",
            caption: "System Name",
            width: 180
        },
        {
            dataField: "SystemUrl",
            caption: "System Url"
        },
        {
            dataField: "SystemApiUrl",
            caption: "System Api Url"
        },
        {
            dataField: "SystemLogin",
            caption: "System Login"
        },
        {
            dataField: "SystemPassword",
            caption: "System Password"
        },
        {
            dataField: "DefaultParentTaskName",
            caption: "Default Parent Task Name"
        },
        {
            dataField: "LastSyncDate",
            caption: "Last Sync Date",
            dataType: 'date',
            width: 100
        }
    ];
    var syncSystemGridOptions = GetDataGridOptions(systemProxyUrl, "SyncSystemCRUD", syncSystemColumns, "SystemId", null, null, gridHeight, true, syncSystemDataSource);

    var syncSystemSettingColumns = [
        {
            dataField: "SettingId",
            allowEditing: false,
            width: 50
        },
        {
            dataField: "Setting",
            caption: "Name"
        }
    ];
    var syncSystemSettingGridOptions = GetDataGridOptions(systemProxyUrl, "SyncSystemSettingCRUD", syncSystemSettingColumns, "SettingId", null, null, gridHeight, true, syncSystemSettingDataSource);

    var syncSystemSettingValueColumns = [
        {
            dataField: "SyncSystemSettingId",
            allowEditing: false,
            width: 50
        },
        {
            dataField: "SystemId",
            caption: "System Name",
            lookup: {
                dataSource: syncSystemDataSource.CustomStore,
                valueExpr: 'SystemId',
                displayExpr: 'SystemName'
            },
            width: 200
        },
        {
            dataField: "SettingId",
            caption: "Setting Name",
            lookup: {
                dataSource: syncSystemSettingDataSource.CustomStore,
                valueExpr: 'SettingId',
                displayExpr: 'Setting'
            },
            width: 200
        },
        {
            dataField: "SettingValue",
            caption: "Value"
        }
    ];
    var syncSystemSettingValueGridOptions = GetDataGridOptions(systemProxyUrl, "SyncSystemSettingValueCRUD", syncSystemSettingValueColumns, "SyncSystemSettingId", null, null, gridHeight, true, null);

    var syncSystemTypeColumns = [
        {
            dataField: "SystemTypeId",
            caption: "System Type Id",
            allowEditing: false
        },
        {
            dataField: "SystemTypeName",
            caption: "System Type Name"
        }
    ];
    var syncSystemTypeGridOptions = GetDataGridOptions(systemProxyUrl, "SyncSystemTypeCRUD", syncSystemTypeColumns, "SystemTypeId", null, null, gridHeight, true, syncSystemTypeDataSource);

    var syncSystemFieldMappingColumns = [
        {
            dataField: "SyncSystemFieldMappingId",
            caption: "Sync System Field Mapping Id",
            allowEditing: false
        },
        {
            dataField: "SystemId",
            caption: "System Name By Id",
            lookup: {
                dataSource: syncSystemDataSource.CustomStore,
                valueExpr: 'SystemId',
                displayExpr: 'SystemName'
            },
            width: 200
        },
        {
            dataField: "SystemFieldName",
            caption: "System Field Name"
        },
        {
            dataField: "EpmFieldName",
            caption: "Emp Field Name"
        },
        {
            dataField: "FieldType",
            caption: "Field Type"
        },
        {
            dataField: "StagingFieldName",
            caption: "Staging Field Name"
        },
        {
            dataField: "IsMultiSelect",
            caption: "Is Multi Select",
            width: 80
        },
        {
            dataField: "IsIdWithValue",
            caption: "Is Id With Value",
            width: 80
        }
    ];
    var syncSystemFieldMappingGridOptions = GetDataGridOptions(systemProxyUrl, "SyncSystemFieldMappingCRUD", syncSystemFieldMappingColumns, "SyncSystemFieldMappingId", null, null, gridHeight, true, null);

    $("#gridContainer__SyncSystem").dxDataGrid(syncSystemGridOptions);
    $("#gridContainer__SyncSystem").dxDataGrid("instance").refresh();
    $("#gridContainer__SyncSystemSetting").dxDataGrid(syncSystemSettingGridOptions);
    $("#gridContainer__SyncSystemSettingValue").dxDataGrid(syncSystemSettingValueGridOptions);
    $("#gridContainer__SyncSystemType").dxDataGrid(syncSystemTypeGridOptions);
    $("#gridContainer__SyncSystemFieldMapping").dxDataGrid(syncSystemFieldMappingGridOptions);
    LinkedItemsTable.BuildGrid(gridHeight);

    syncSystemGridOptions.CustomStore.ParentGrid = $("#gridContainer__SyncSystem").dxDataGrid("instance");
    syncSystemSettingGridOptions.CustomStore.ParentGrid = $("#gridContainer__SyncSystemSetting").dxDataGrid("instance");
    syncSystemSettingValueGridOptions.CustomStore.ParentGrid = $("#gridContainer__SyncSystemSettingValue").dxDataGrid("instance");
    syncSystemTypeGridOptions.CustomStore.ParentGrid = $("#gridContainer__SyncSystemType").dxDataGrid("instance");
    syncSystemFieldMappingGridOptions.CustomStore.ParentGrid = $("#gridContainer__SyncSystemFieldMapping").dxDataGrid("instance");

    //LoadPanelHide();
}

function ButtonMergeDbToEpmClick() {
    LoadPanelShow();
    $.ajax({
        url: systemProxyUrl + "Jira/ButtonMergeDbToEpm",
        method: "GET",
        dataType: "json",
        success: function (response) {
            LoadPanelHide();
            debugger;
        },
        error: function (jqXhr, textStatus, error) {
            LoadPanelHide();
            debugger;
        }
    });
}