var LinkedItemsTable = {
    RefershGrid: function () {
        $("#gridContainer__VProjectServerSystemLink").dxDataGrid("instance").refresh();
    },
    BuildGrid: function (gridHeight, onLoadComplete) {
        var vProjectServerSystemLinkSource = { CustomStore: [] };
        var vProjectServerSystemLinkColumns = [
            {
                dataField: "SystemName",
                caption: "System Name",
                allowEditing: false,
                groupIndex: 0
            },
            {
                dataField: "ProjectUid",
                caption: "EPM Project Uid"
            },
            {
                dataField: "EpmProjectName",
                caption: "EPM Project Name"
            },
            {
                dataField: "ProjectId",
                caption: "Sytem Project Name",
                allowEditing: false
            },
            {
                dataField: "ProjectName",
                caption: "Sytem Project Name",
                allowEditing: false
            },
            {
                dataField: "EpicKey",
                caption: "Epic Key",
                allowEditing: false
            },
            {
                dataField: "EpicName",
                caption: "Epic Name",
                allowEditing: false
            },
            {
                dataField: "IsHomeProject",
                caption: "Home Project",
                allowEditing: false,
                dataType: "boolean",
                width: 60
            },
            {
                dataField: "LastExecuted",
                caption: "Last Execute Date",
                dataType: 'date',
                width: 120
            },
            {
                dataField: "RowNumber",
                caption: "",
                allowEditing: true,
                cellTemplate: function (cellElement, cellInfo) {
                    var currentRow = cellInfo.data;
                    var btnMod = $("<div/>");
                    btnMod.attr("id", "btnMod_" + cellInfo.rowIndex);
                    btnMod.addClass("execute-button");
                    btnMod.click(function (e) {
                        document.getElementById('btnSpinner_' + cellInfo.rowIndex).style.display = 'inline-block';
                        var syncAllPath;
                        switch (currentRow.SystemName) {
                            case "JIRA Cloud":
                                syncAllPath = "Jira/SyncAll";
                                break;
                            case "Visual Studio Team Services":
                                syncAllPath = "Tfs/Execute";
                                break;
                            default:
                                syncAllPath = "Jira/SyncAll";
                                break;
                        }
                        return $.ajax({
                            url: systemProxyUrl + syncAllPath,
                            method: "POST",
                            dataType: "json",
                            data: currentRow,
                            success: function (result1) {
                                if (result1.Result === "ok") {
                                    document.getElementById('btnSpinner_' + cellInfo.rowIndex).style.display = 'none';
                                    LinkedItemsTable.RefershGrid();
                                } else {
                                    document.getElementById('btnSpinner_' + cellInfo.rowIndex).style.display = 'none';
                                    LinkedItemsTable.RefershGrid();
                                }
                            },
                            error: function (jqXhr, textStatus, error) {
                                document.getElementById('btnSpinner_' + cellInfo.rowIndex).style.display = 'none';
                                HandleErrorExecuteJiraRequest(jqXhr, textStatus, error);
                                LinkedItemsTable.RefershGrid();
                            }
                        });
                    });
                    btnMod.text("Execute");
                    btnMod.appendTo(cellElement);
                    var btnSpinner = $("<div class='dx-loadpanel-indicator dx-loadindicator dx-widget button-spinner'><div class='dx-loadindicator-wrapper'><div class='dx-loadindicator-content'><div class='dx-loadindicator-icon'><div class='dx-loadindicator-segment dx-loadindicator-segment7'></div><div class='dx-loadindicator-segment dx-loadindicator-segment6'></div><div class='dx-loadindicator-segment dx-loadindicator-segment5'></div><div class='dx-loadindicator-segment dx-loadindicator-segment4'></div><div class='dx-loadindicator-segment dx-loadindicator-segment3'></div><div class='dx-loadindicator-segment dx-loadindicator-segment2'></div><div class='dx-loadindicator-segment dx-loadindicator-segment1'></div><div class='dx-loadindicator-segment dx-loadindicator-segment0'></div></div></div></div></div>");
                    btnSpinner.attr("id", "btnSpinner_" + cellInfo.rowIndex);
                    btnSpinner.css("display", "none");
                    btnSpinner.appendTo(cellElement);
                }
            }
        ];
        var vProjectServerSystemLinkGridOptions = GetDataGridOptions(systemProxyUrl, "ProjectServerSystemLinkCRUD", vProjectServerSystemLinkColumns, "RowNumber",
            { projectUid: currentProjectUid }, null, gridHeight, false, vProjectServerSystemLinkSource);
        $("#gridContainer__VProjectServerSystemLink").dxDataGrid(vProjectServerSystemLinkGridOptions);
        $("#gridContainer__VProjectServerSystemLink").dxDataGrid("instance").beginCustomLoading();

        CommonAjaxRequest(systemProxyUrl + "ProjectServerSystemLinkCRUD" + "/GetProjectServerSystemLinks",
            { projectUid: currentProjectUid },
            "GET",
            null,
            function (result) {
                var resultObject = $.parseJSON(result.Data);
                $("#gridContainer__VProjectServerSystemLink").dxDataGrid("instance")
                    .option("dataSource", resultObject.Linked.ProjectServerSystemLinks);
                var gridInstance = $("#gridContainer__VProjectServerSystemLink").dxDataGrid("instance");
                gridInstance.endCustomLoading();
                if (currentProjectUid != null) {
                    $("#gridContainer__VProjectServerSystemLink").dxDataGrid('columnOption', 'ProjectUid', 'visible', false);
                    $("#gridContainer__VProjectServerSystemLink").dxDataGrid('columnOption', 'EpmProjectName', 'visible', false);
                }
                if (onLoadComplete != null) {
                    onLoadComplete(result);
                }
            },
            function (jqXhr, textStatus, error) {
                CommonAjaxError(jqXhr, textStatus, error);
            });
    }
};