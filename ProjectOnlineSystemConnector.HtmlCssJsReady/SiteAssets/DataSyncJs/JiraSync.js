var syncSystemTypeJira = 1;
//var syncSystemTypePivotalTracker = 2;
var syncSystemTFS = 2;

//come from our server
var currentSystem;
var systems = [];
var objectLinked = {}, objectToBlock = {};
var selectedJiraEpics = [], jiraEpicsToBlock = [];


//come from JIRA
var jiraProjects = [];
var jiraEpics = [];

var jiraCustomFieldEpicNameName = "epic name";

var jiraIssueTypeEpicId;

var jiraEpicNameFieldId;

//controls
var systemsControl, jiraIsHomeProjectControl, jiraProjectsControl, jiraEpicsControl;
var buttonSaveControl, buttonExecuteControl, buttonUpdateAssignmentsControl;
var buttonSaveControlTooltip, buttonExecuteControlTooltip, buttonUpdateAssignmentsControlTooltip;

var allColumns = ['EpicKey', 'EpicName', 'ProjectKey', 'ProjectName'];

var isFirstProjectsLoad;

var isProjectsLoaded = false;
var isEpicsLoaded = false;

var itemsPerPage = 1000;
var counter = 0;

//TFS 

var tfsProjects = [];
var tfsEpics = [];
var tfsEpicsToBlock = [], selectedTfsEpics = [];

//var defaultColumns = ['System.Id', 'System.Title', "System.State", "System.CreatedDate"];

var psCustomFieldSyncObjectLinkName = "SixtyI_SyncObjectLink";

var psCustomFields, psCustomFieldSyncObjectLink;
var psCurrentProject, includeCustomFields;;
var psDraftAssignments;
var psDraftProject;
var psDraftProjectUser;

var originalSyncObjectLink;
var newSyncObjectLinkArray = [];

var tfsCustomFields;

var columnsTypes = {};

var tfsEpicsControl, buttonPublishControl;


function ExecuteJiraRequest(apiUrl, postData, requestType, systemId, functionSuccess, functionError) {
    LoadPanelShow();
    return $.ajax({
        url: systemProxyUrl + "Jira/ExecuteRequest",
        method: "POST",
        dataType: "json",
        data: {
            ApiUrl: apiUrl,
            RequestType: requestType,
            SystemId: systemId,
            PostData: postData
        },
        success: function (data) {
            if (functionSuccess != null) {
                functionSuccess(data);
            }
        },
        error: function (jqXhr, textStatus, error) {
            if (functionError != null) {
                functionError(jqXhr, textStatus, error);
            }
        }
    });
}

function HandleErrorExecuteJiraRequest(jqXhr, textStatus, error) {
    LoadPanelHide();
    if (jqXhr.responseJSON != null) {
        alert(error + ". " + jqXhr.responseJSON.Exception.Message);
    } else {
        alert('An error occurred... Look at the console (F12 or Ctrl+Shift+I, Console tab) for more information!');
    }
}

function RequestExecutorError(responseInfo, code, statusText) {
    LoadPanelHide();
    alert('An error occurred... Look at the console (F12 or Ctrl+Shift+I, Console tab) for more information! \n code: ' + code, + '; status text: ' + statusText);

}

$(document).ready(function () {
    debugger;
    DocumentReady();
});

function SetTooltip(toolTipeName, controlName, toolTipText) {
    var controlTooltip = $("#" + toolTipeName).dxTooltip({
        target: "#" + controlName,
        contentTemplate: function (contentElement) {
            contentElement.append("<p>" + toolTipText + "</p>");
        }
    }).dxTooltip("instance");
    $("#" + controlName).mouseover(function () {
        controlTooltip.show();
    });
    $("#" + controlName).mouseout(function () {
        controlTooltip.hide();
    });
}

function DocumentReady() {
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

    buttonSaveControl = $("#ButtonSave").dxButton({
        text: "Save",
        disabled: true,
        onClick: ButtonSaveClick
    }).dxButton("instance");
    SetTooltip("ButtonSaveTooltip", "ButtonSave", "Use this option to build your Multi-methodology project plan.");

    buttonExecuteControl = $("#ButtonExecute").dxButton({
        text: "Execute",
        visible: false,
        onClick: ButtonExecuteClick
    }).dxButton("instance");
    SetTooltip("ButtonExecuteTooltip", "ButtonExecute", "Establishes integrated plan.");

    buttonUpdateAssignmentsControl = $("#ButtonUpdateAssignments").dxButton({
        text: "Update Assignments",
        disabled: false,
        visible: false,
        onClick: ButtonUpdateAssignmentsClick
    }).dxButton("instance");
    SetTooltip("ButtonUpdateAssignmentsTooltip", "ButtonUpdateAssignments", "Merge Agile and EPM Resources.");

    systemsControl = $("#Systems").dxSelectBox({
        dataSource: new DevExpress.data.ArrayStore({
            data: systems,
            key: "SystemId"
        }),
        displayExpr: "SystemName",
        valueExpr: "SystemId",
        onValueChanged: SystemsSelectionChanged,
        searchEnabled: true
    }).dxSelectBox("instance");
    SetTooltip("SystemsTooltip", "Systems", "Select the source of in which the Product/ Project resides.");

    jiraIsHomeProjectControl = $("#JiraIsHomeProject").dxCheckBox({
        text: "",
        onValueChanged: function (data) {
            isFirstProjectsLoad = false;
            if (data.value === true) {
                jiraProjectsControl.option('disabled', false);
                saveButtonEnablerFix();
            }
            else {
                jiraProjectsControl.option('disabled', true);
                jiraProjectsControl.reset();
                saveButtonEnablerFix();
            }
        },
        disabled: true
    }).dxCheckBox("instance");
    SetTooltip("JiraIsHomeProjectTooltip", "JiraIsHomeProject", "Select Checkbox to capture all EPICs and data associated with the selected Project/ Product.");

    jiraProjectsControl = $("#JiraProjects").dxSelectBox({
        dataSource: new DevExpress.data.ArrayStore({
            data: jiraProjects,
            key: "ProjectId"
        }),
        onValueChanged: function (data) {
            saveButtonEnablerFix();
        },
        displayExpr: "ProjectName",
        valueExpr: "ProjectId",
        disabled: true,
        searchEnabled: true
    }).dxSelectBox("instance");
    SetTooltip("JiraProjectsTooltip", "JiraProjects", "Select desired Project/ Product.");

    jiraEpicsControl = $("#JiraEpics").dxDataGrid({
        dataSource: new DevExpress.data.ArrayStore({
            data: jiraEpics,
            key: 'EpicKey'
        }),
        selection: {
            mode: "multiple",
            showCheckBoxesMode: "none"
        },
        headerFilter: {
            visible: true
        },
        filterRow: {
            visible: true,
            applyFilter: "auto"
        },
        paging: {
            enabled: true
        },
        columnChooser: {
            enabled: false
        },
        columnFixing: {
            enabled: true
        },
        onContentReady: function (e) {
            jiraEpicsControl.selectRows(selectedJiraEpics);

            for (var i = 0; i < jiraEpicsToBlock.length; ++i) {
                if (jiraEpicsControl.getRowIndexByKey(jiraEpicsToBlock[i]) !== -1
                    && jiraEpicsControl.getCellElement(jiraEpicsControl.getRowIndexByKey(jiraEpicsToBlock[i]), 0) != null) {
                    jiraEpicsControl.getCellElement(jiraEpicsControl.getRowIndexByKey(jiraEpicsToBlock[i]), 0).children().hide();
                }
            }
            saveButtonEnablerFix();
        },
        onSelectionChanged: function (e) {
            selectedJiraEpics = [];
            $.each(e.selectedRowKeys, function (key, epicKey) {
                selectedJiraEpics.push(epicKey);
            });
            e.component.deselectRows(jiraEpicsToBlock);
            saveButtonEnablerFix();
        },
        onRowPrepared: function (info) {
            if (info.rowType !== 'header') {
                var value = GetValue(info.data, ["EpicKey"]);
                if (value == null || value === '') {
                    return;
                }
                $.each(jiraEpicsToBlock, function (key1, jiraEpic) {
                    if (jiraEpic === value) {
                        info.rowElement.css('color', 'silver');
                    }
                });
            }
            saveButtonEnablerFix();
        },
        allowColumnReordering: true,
        allowColumnResizing: true,
        columns: allColumns
    }).dxDataGrid("instance");


    //TFS
    buttonPublishControl = $("#ButtonPublish").dxButton({
        text: "Publish",
        disabled: true,
        onClick: PublishClick
    }).dxButton("instance");

    systemsControl = $("#Systems").dxSelectBox({
        dataSource: new DevExpress.data.ArrayStore({
            data: systems,
            key: "SystemId"
        }),
        displayExpr: "SystemName",
        valueExpr: "SystemId",
        onValueChanged: SystemsSelectionChanged,
        searchEnabled: true
    }).dxSelectBox("instance");

    tfsEpicsControl = $("#TfsEpics").dxDataGrid({
        dataSource: new DevExpress.data.ArrayStore({
            data: tfsEpics,
            key: 'System.Id'
        }),
        selection: {
            mode: "multiple",
            showCheckBoxesMode: "none"
        },
        paging: {
            enabled: true
        },
        columnChooser: {
            enabled: true
        },
        columnFixing: {
            enabled: true
        },
        onContentReady: function (e) {

            tfsEpicsControl.selectRows(selectedTfsEpics);

            for (var i = 0; i < tfsEpicsToBlock.length; ++i) {
                if (tfsEpicsControl.getRowIndexByKey(+tfsEpicsToBlock[i]) != -1
                    && tfsEpicsControl.getCellElement(tfsEpicsControl.getRowIndexByKey(+tfsEpicsToBlock[i]), 0) != null) {
                    tfsEpicsControl.getCellElement(tfsEpicsControl.getRowIndexByKey(+tfsEpicsToBlock[i]), 0).children().hide();
                }
            }
        },
        onSelectionChanged: function (e) {
            selectedTfsEpics = [];
            $.each(e.selectedRowKeys, function (key, epicKey) {
                selectedTfsEpics.push(epicKey);
            });
            $.each(tfsEpicsToBlock, function (key, tfsEpic) {
                if (tfsEpic.SystemId === currentSystem.SystemId) {
                    e.component.deselectRows(tfsEpic.Epics);
                    return;
                }
            });
        },
        onRowPrepared: function (info) {
            if (info.rowType !== 'header') {
                var value = GetValue(info.data, ["System", "Id"]);
                if (value == null || value === '') {
                    return;
                }
                $.each(tfsEpicsToBlock, function (key, tfsEpicToBlock) {
                    if (tfsEpicToBlock.SystemId === currentSystem.SystemId) {
                        $.each(tfsEpicToBlock.Epics, function (key1, tfsEpic) {
                            if (tfsEpic == value) {
                                info.rowElement.css('color', 'silver');
                            }
                        });
                        return;
                    }
                });
            }
        },
        allowColumnReordering: true,
        allowColumnResizing: true,
        columns: allColumns
    }).dxDataGrid("instance");

    tfsEpicsControl.option("visible", false);

    InitApplication();
    window.LoadPanelShow();
    GetCurrentUser(GetUserInfoSucceeded, ExecuteQueryAsyncFail);
}

function ButtonExecuteClick(data) {
    if (currentSystem != null) {
        ButtonSaveClick(null, ExecuteAction);
    } else {
        ToggleEdit(false);
        ExecuteAction();
    }
}

function ExecuteAction() {
    //alert("Execute action is in progress. When Sync process is complete, the changes will be reflected in the Schedule. Thank you!");
    window.LoadPanelHide();
    $.ajax({
        url: systemProxyUrl + "Jira/SyncAll",
        method: "POST",
        dataType: "json",
        data: {
            ProjectUid: currentProjectUid
        },
        success: function (response) {
            if (response.Result !== "ok") {
                alert(response.Data);
            }
            ToggleEdit(true);
            //alert("Execute action finished");
        },
        error: function (jqXhr, textStatus, error) {
            //HandleErrorExecuteJiraRequest(jqXhr, textStatus, error);
        }
    });
}

function GetValue(obj, keyPath) {
    if (obj == null) {
        return null;
    }
    var lastKeyIndex = keyPath.length - 1;
    for (var i = 0; i < lastKeyIndex; ++i) {
        var key = keyPath[i];
        if (!(key in obj))
            obj[key] = {}
        obj = obj[key];
    }
    return obj[keyPath[lastKeyIndex]];
}

function GetUserInfoSucceeded(sender, args) {
    GetCurrentProjectUid(GetCurrentProjectUidSucceeded);
}

function GetCurrentProjectUidSucceeded() {
    if (currentProjectUid == null || currentProjectUid === guidEmpty) {
        return;
    }
    GetFormDigestValue(GetFormDigestValueSuccess);
    GetProjectOnlineData();
}

function GetFormDigestValueSuccess() {
    GetSyncSettings(GetSyncSettingsSuccess);
}

function OnGetLogMessage(message) {
    debugger;
}

function GetSyncSettingsSuccess() {
    //SetUpSignalRHub(OnGetLogMessage, currentProjectUid);
    LinkedItemsTable.BuildGrid(400,
        function (result) {
            if (result.Result === "ok") {
                var resultObject = $.parseJSON(result.Data);
                objectLinked = resultObject.Linked;
                objectToBlock = resultObject.ToBlock;
                GetJiraSystems();
            }
        });
}

function GetJiraSystems() {
    window.LoadPanelShow();
    return $.ajax({
        url: systemProxyUrl + 'SyncSystemCRUD/GetSyncSystemListAsync',
        method: "GET",
        dataType: "json",
        success: function (data) {
            window.LoadPanelHide();
            systems = $.parseJSON(data.Data);
            systemsControl.option('dataSource', new DevExpress.data.ArrayStore({ data: systems, key: "SystemId" }));
        },
        error: function (jqXhr, textStatus, error) {
            window.LoadPanelHide();
            HandleErrorExecuteJiraRequest(jqXhr, textStatus, error);
        }
    });
}

function SystemsSelectionChanged(data) {
    window.LoadPanelShow();
    var currentSystems = $.grep(systems, function (system, i) {
        return system.SystemId === data.value;
    });
    currentSystem = currentSystems[0];

    jiraIsHomeProjectControl.option('value', false);
    jiraProjectsControl.reset();
    jiraEpicsControl.deselectAll();

    isFirstProjectsLoad = true;
    jiraProjects = [];
    jiraEpics = [];
    jiraEpicNameFieldId = null;
    selectedJiraEpics = [];

    isProjectsLoaded = false;
    isEpicsLoaded = false;

    itemsPerPage = 1000;
    counter = 0;

    tfsEpics = [];
    selectedTfsEpics = [];
    tfsEpicsToBlock = [];

    var objectsLinkedCurrentSystem = $.grep(objectLinked.ProjectServerSystemLinks, function (syncObjectLink, syncObjectLinkIndex) {
        return syncObjectLink.SystemId === currentSystem.SystemId && syncObjectLink.EpicKey != null;
    });


    var objectsToBlockCurrentSystem = $.grep(objectToBlock.ProjectServerSystemLinks, function (syncObjectLink, syncObjectLinkIndex) {
        return syncObjectLink.SystemId === currentSystem.SystemId && syncObjectLink.EpicKey != null;
    });


    switch (currentSystem.SystemTypeId) {
        case syncSystemTypeJira:
            $.each(objectsLinkedCurrentSystem, function (key, syncObjectLink) {
                selectedJiraEpics.push(syncObjectLink.EpicKey);
            });

            $.each(objectsToBlockCurrentSystem, function (key, syncObjectLink) {
                jiraEpicsToBlock.push(syncObjectLink.EpicKey);
            });

            jiraEpicsControl.option("visible", true);
            tfsEpicsControl.option("visible", false);
            $.when(GetJiraCustomFields(), GetJiraIssueTypes()).done(GetJiraDataSuccess);
            break;
        case syncSystemTFS:
            $.each(objectsLinkedCurrentSystem, function (key, syncObjectLink) {
                selectedTfsEpics.push(syncObjectLink.EpicKey);
            });

            $.each(objectsToBlockCurrentSystem, function (key, syncObjectLink) {
                tfsEpicsToBlock.push(syncObjectLink.EpicKey);
            });

            jiraEpicsControl.option("visible", false);
            tfsEpicsControl.option("visible", true);

            GetTfsData();

            break;
        default:
            window.LoadPanelHide();
            break;
    }

    saveButtonEnablerFix();
}

function GetPTProjects() {
    return $.ajax({
        url: systemProxyUrl + 'PivotalTracker/GetProjects?systemId=' + currentSystem.SystemId,
        method: "POST",
        dataType: "json",
        success: function (data) {
            window.LoadPanelHide();
            //jiraProjects = data;
            jiraProjects = [];
            for (var i = 0; i < data.length; i += 1) {
                var b = {};
                b.ProjectId = data[i]['Id'];
                b.ProjectName = data[i]['Name'];
                jiraProjects.push(b);
            }
            jiraProjectsControl.option('dataSource', new DevExpress.data.ArrayStore({
                data: jiraProjects,
                key: "ProjectId"
            }));
            jiraProjectsControl.option('displayExpr', "ProjectName");
            jiraProjectsControl.option('valueExpr', "ProjectId");
            ToggleEdit(true);
            // systemsControl.option('dataSource', new DevExpress.data.ArrayStore({ data: systems, key: "SystemId" }));
            //systemsControl.option('value', systems[0].SystemId);
        },
        error: function (jqXhr, textStatus, error) {
            console.log(jqXhr);
            console.log(textStatus);
            console.log(error);
            window.LoadPanelHide();

            HandleErrorExecuteJiraRequest(jqXhr, textStatus, error);
        }
    });
}

function GetJiraCustomFields() {
    return ExecuteJiraRequest("field", null, "GET", currentSystem.SystemId, null, null);
}

function GetJiraIssueTypes() {
    return ExecuteJiraRequest("issuetype", null, "GET", currentSystem.SystemId, null, null);
}

function GetJiraDataSuccess(responseCustomFields, responseIssueTypes) {
    window.LoadPanelShow();
    var customFields = $.parseJSON(responseCustomFields[0].Data);
    var issueTypes = $.parseJSON(responseIssueTypes[0].Data);

    $.each(customFields, function (key, customField) {
        if (customField.name.toLowerCase() === jiraCustomFieldEpicNameName) {
            jiraEpicNameFieldId = customField.id;
        }
    });

    if (jiraEpicNameFieldId == null) {
        jiraEpicNameFieldId = 'summary';
    }

    $.each(issueTypes, function (key, issueType) {
        if (issueType.name.toLowerCase() === "epic") {
            jiraIssueTypeEpicId = issueType.id;
        }
    });
    if (jiraIssueTypeEpicId == null) {
        alert("There is no issues with type 'Epic' in JIRA");
        ToggleEdit(false);
        return;
    }

    window.LoadPanelShow();
    GetProjects();
    GetEpics();

    var intervalId = setInterval(
        function () {
            if (isEpicsLoaded && isProjectsLoaded) {
                clearInterval(intervalId);
                jiraEpicsControl.option('dataSource', new DevExpress.data.ArrayStore({
                    data: jiraEpics,
                    key: 'EpicKey'
                }));
                jiraEpicsControl.refresh();
                ToggleEdit(true);
                window.LoadPanelHide();
            }
        }, 2000);
}

function GetProjects() {
    ExecuteJiraRequest("project?maxResults=1000&startAt=0&expand=url", null, "GET", currentSystem.SystemId, GetProjectsSuccess, HandleErrorExecuteJiraRequest);
}

function GetProjectsSuccess(responseProjects) {
    window.LoadPanelShow();
    var jiraProjectsTemp = $.parseJSON(responseProjects.Data);

    var objectsToBlockCurrentSystem = $.grep(objectToBlock.ProjectServerSystemLinks, function (syncObjectLink, i) {
        return syncObjectLink.SystemId === currentSystem.SystemId && syncObjectLink.IsHomeProject && syncObjectLink.ProjectKey != null;
    });

    $.each(jiraProjectsTemp, function (key, project) {
        if (objectsToBlockCurrentSystem.length === 0) {
            jiraProjects.push({ 'ProjectKey': project.key, 'ProjectId': project.id, 'ProjectName': project.name });
        }
        else {
            var projectsToBlock = $.grep(objectsToBlockCurrentSystem, function (objectToBlockCurrentSystem, i) {
                return objectToBlockCurrentSystem.ProjectId === project.id;
            });
            if (projectsToBlock.length === 0) {
                jiraProjects.push({ 'ProjectKey': project.key, 'ProjectId': project.id, 'ProjectName': project.name });
            }
        }
    });

    jiraProjectsControl.option('dataSource', new DevExpress.data.ArrayStore({
        data: jiraProjects,
        key: "ProjectId"
    }));
    jiraProjectsControl.option('displayExpr', "ProjectName");
    jiraProjectsControl.option('valueExpr', "ProjectId");

    var objectsLinkedCurrentSystem = $.grep(objectLinked.ProjectServerSystemLinks, function (syncObjectLink, i) {
        return syncObjectLink.SystemId === currentSystem.SystemId && syncObjectLink.IsHomeProject && syncObjectLink.ProjectId != null;
    });
    if (objectsLinkedCurrentSystem != null && objectsLinkedCurrentSystem.length !== 0) {
        jiraIsHomeProjectControl.option('value', objectsLinkedCurrentSystem[0].IsHomeProject);
        jiraProjectsControl.option('value', objectsLinkedCurrentSystem[0].ProjectId);
    }
    isProjectsLoaded = true;
}

function GetEpics() {
    ExecuteJiraRequest("search?maxResults=" + itemsPerPage + "&startAt=" + counter + "&jql=issuetype=" + jiraIssueTypeEpicId, null, "GET", currentSystem.SystemId, GetEpicsSuccess, HandleErrorExecuteJiraRequest);
}

function GetEpicsSuccess(responseEpics) {
    window.LoadPanelShow();

    var jiraEpicsTemp = $.parseJSON(responseEpics.Data);
    counter += jiraEpicsTemp.maxResults;
    $.each(jiraEpicsTemp.issues, function (key, issue) {
        var objToPush = {};
        objToPush['EpicKey'] = issue.key;
        objToPush['EpicId'] = issue.id;
        objToPush['EpicName'] = issue.fields[jiraEpicNameFieldId];
        if (objToPush['EpicName'] == null) {
            objToPush['EpicName'] = issue.fields['summary'];
        }
        objToPush['ProjectKey'] = issue.fields.project.key;
        objToPush['ProjectName'] = issue.fields.project.name;
        objToPush['ProjectId'] = issue.fields.project.id;
        jiraEpics.push(objToPush);
    });

    if (jiraEpicsTemp.total > counter) {
        ExecuteJiraRequest("search?maxResults=" + itemsPerPage + "&startAt=" + counter + "&jql=issuetype=" + jiraIssueTypeEpicId, null, "GET", currentSystem.SystemId, GetEpicsSuccess, HandleErrorExecuteJiraRequest);
    } else {
        isEpicsLoaded = true;
    }
}

function ToggleEdit(isEditable) {

    if (!isEditable) {
        buttonExecuteControl.option('disabled', true);
        buttonSaveControl.option('disabled', true);
        //buttonUpdateAssignmentsControl.option('disabled', true);

        jiraIsHomeProjectControl.option('disabled', true);
        jiraProjectsControl.option('disabled', true);
        jiraEpicsControl.option('selection', {
            mode: "multiple",
            showCheckBoxesMode: "none"
        });
        tfsEpicsControl.option('selection', {
            mode: "multiple",
            showCheckBoxesMode: "none"
        });
    } else {
        buttonExecuteControl.option('disabled', false);

        if (systemsControl.option('value') != null) {
            buttonSaveControl.option('disabled', false);
            saveButtonEnablerFix();
        } else {
            buttonSaveControl.option('disabled', true);
        }

        buttonSaveControl.option('disabled', false);
        //buttonUpdateAssignmentsControl.option('disabled', false);

        jiraIsHomeProjectControl.option('disabled', false);
        saveButtonEnablerFix();
        if (jiraIsHomeProjectControl.option('value') === true) {
            jiraProjectsControl.option('disabled', false);
            saveButtonEnablerFix();
        }
        else {
            jiraProjectsControl.option('disabled', true);
        }
        jiraEpicsControl.option('selection', {
            mode: "multiple",
            showCheckBoxesMode: "always"
        });
        tfsEpicsControl.option('selection', {
            mode: "multiple",
            showCheckBoxesMode: "always"
        });
        saveButtonEnablerFix();
    }
}

function ButtonSaveClick(data, action) {
    if (jiraIsHomeProjectControl.option('value') === true && jiraProjectsControl.option('value') == null) {
        alert('You have choosen Link Product. Please Select Jira project');
        return -1;
    }
    ToggleEdit(false);
    LoadPanelShow();

    var syncObjectLink = {};
    syncObjectLink['ProjectUid'] = currentProjectUid;
    syncObjectLink['SystemId'] = currentSystem.SystemId;
    syncObjectLink['IsHomeProject'] = jiraIsHomeProjectControl.option('value');
    var currentProjects;
    switch (currentSystem.SystemTypeId) {
        case syncSystemTypeJira:
            currentProjects = $.grep(jiraProjects, function (jiraProject, i) {
                return jiraProject.ProjectId === jiraProjectsControl.option('value');
            });
            if (currentProjects != null && currentProjects.length !== 0) {
                syncObjectLink['ProjectKey'] = currentProjects[0].ProjectKey;
                syncObjectLink['ProjectName'] = currentProjects[0].ProjectName;
                syncObjectLink['ProjectId'] = currentProjects[0].ProjectId;
            }
            else {
                syncObjectLink['ProjectKey'] = null;
                syncObjectLink['ProjectName'] = null;
                syncObjectLink['ProjectId'] = null;
            }

            var jiraEpicsToSend = $.grep(jiraEpics, function (jiraEpic, i) {
                return selectedJiraEpics.indexOf(jiraEpic.EpicKey) >= 0;
            });

            syncObjectLink['Epics'] = jiraEpicsToSend;

            return $.ajax({
                url: systemProxyUrl + "ProjectServerSystemLinkCRUD/LinkEpmToSystem",
                method: "POST",
                dataType: "json",
                data: syncObjectLink,
                success: function (result) {
                    LinkedItemsTable.RefershGrid();
                    if (result.Result === "ok") {
                        if (action != null) {
                            action();
                        } else {
                            ToggleEdit(true);
                            window.LoadPanelHide();
                        }
                    } else {
                        ToggleEdit(true);
                        alert(result.Data);
                        window.LoadPanelHide();
                    }
                },
                error: function (jqXhr, textStatus, error) {
                    HandleErrorExecuteJiraRequest(jqXhr, textStatus, error);
                    window.LoadPanelHide();
                }
            });
        case syncSystemTFS:
            currentProjects = $.grep(jiraProjects, function (jiraProject, i) {
                return jiraProject.ProjectId === jiraProjectsControl.option('value');
            });
            if (currentProjects != null && currentProjects.length !== 0) {
                syncObjectLink['ProjectKey'] = currentProjects[0].ProjectKey;
                syncObjectLink['ProjectName'] = currentProjects[0].ProjectName;
                syncObjectLink['ProjectId'] = currentProjects[0].ProjectId;
            }
            else {
                syncObjectLink['ProjectKey'] = null;
                syncObjectLink['ProjectName'] = null;
                syncObjectLink['ProjectId'] = null;
            }

            var tfsEpicsToSend = $.grep(tfsEpics, function (tfsEpic, i) {
                return selectedTfsEpics.indexOf(tfsEpic.System.Id) >= 0;
            });

            syncObjectLink['Epics'] = tfsEpicsToSend;

            return $.ajax({
                url: systemProxyUrl + "ProjectServerSystemLinkCRUD/LinkEpmToSystem",
                method: "POST",
                dataType: "json",
                data: syncObjectLink,
                success: function (result) {
                    LinkedItemsTable.RefershGrid();
                    if (result.Result === "ok") {
                        if (action != null) {
                            action();
                        } else {
                            ToggleEdit(true);
                            window.LoadPanelHide();
                        }
                    } else {
                        ToggleEdit(true);
                        alert(result.Data);
                        window.LoadPanelHide();
                    }
                },
                error: function (jqXhr, textStatus, error) {
                    HandleErrorExecuteJiraRequest(jqXhr, textStatus, error);
                    window.LoadPanelHide();
                }
            });

            break;
        default:
            window.LoadPanelHide();
            break;
    }

}

function ButtonUpdateAssignmentsClick(data) {
    ToggleEdit(false);
    buttonUpdateAssignmentsControl.option('disabled', true);
    //LoadPanelShow();
    return $.ajax({
        url: systemProxyUrl + 'Common/AssignAllResources?projectUid=' + currentProjectUid,
        method: "GET",
        dataType: "json",
        success: function (result) {
            if (result.Result === "ok") {
                ToggleEdit(true);
                window.LoadPanelHide();
                buttonUpdateAssignmentsControl.option('disabled', false);
                //ButtonExecuteClick();
            } else {
                ToggleEdit(true);
                alert(result.Data);
                buttonUpdateAssignmentsControl.option('disabled', false);
                //window.LoadPanelHide();
            }
        },
        /*error: function (jqXhr, textStatus, error) {
            HandleErrorExecuteJiraRequest(jqXhr, textStatus, error);
            //window.LoadPanelHide();
        }*/
    });

}

function saveButtonEnablerFix() {
    buttonSaveControl.option('disabled', true);
    if (systemsControl.option('value') !== null) {
        if (jiraIsHomeProjectControl.option('value') === true) {
            if (jiraProjectsControl.option('value') !== null) {
                buttonSaveControl.option('disabled', false);
            }
        } else {
            //    if (jiraEpicsControl._options.selectedRowKeys.length > 0) {
            buttonSaveControl.option('disabled', false);
            //    }
        }
    }
}


// TFS

function ExecuteTfsRequest(apiUrl, postData, requestType, system, functionSuccess, functionError) {
    LoadPanelShow();
    var contentType = 'application/json';
    if (requestType.toLowerCase() === "patch") {
        contentType = 'application/json-patch+json';
    }
    return $.ajax({
        url: system.SystemApiUrl + apiUrl,
        method: requestType,
        dataType: "json",
        headers: {
            'Accept': 'application/json',
            'Content-Type': contentType,
            'Authorization': 'Basic ' + btoa("" + ":" + system.SystemPassword)
        },
        data: postData,
        success: function (data) {
            if (functionSuccess != null) {
                functionSuccess(data);
            }
        },
        error: function (jqXhr, textStatus, error) {
            if (functionError != null) {
                functionError(jqXhr, textStatus, error);
            }
        }
    });
}

function HandleErrorExecuteJiraRequest(jqXhr, textStatus, error) {
    var response = $.parseJSON(jqXhr.responseText);
    if (response != null) {
        alert(error + ". " + response.message);
    }
}



function GetProjectOnlineData() {
    window.LoadPanelShow();

    psCustomFields = projContext.get_customFields();
    psCurrentProject = projContext.get_projects().getByGuid(currentProjectUid);
    includeCustomFields = psCurrentProject.get_includeCustomFields();

    projContext.load(psCustomFields);
    projContext.load(psCurrentProject);
    projContext.load(includeCustomFields);
    window.LoadPanelShow();
    projContext.executeQueryAsync(GetDataQuerySucceeded, ExecuteQueryAsyncFail);
}

function GetDataQuerySucceeded(sender, args) {
    window.LoadPanelShow();
    var customFieldsEnumerator = psCustomFields.getEnumerator();
    while (customFieldsEnumerator.moveNext()) {
        if (customFieldsEnumerator.get_current().get_name() === psCustomFieldSyncObjectLinkName) {
            psCustomFieldSyncObjectLink = customFieldsEnumerator.get_current();
        }
    }

    originalSyncObjectLink = psCurrentProject.get_includeCustomFields().get_fieldValues()[psCustomFieldSyncObjectLink.get_internalName()];
    GetProjectOData();
}

function GetProjectOData() {
    requestExecutor.executeAsync({
        url: appWebUrl + "/_api/ProjectData/Projects?$select=ProjectId,ProjectName," + psCustomFieldSyncObjectLinkName
        + "&$filter=" + psCustomFieldSyncObjectLinkName + " ne null and ProjectId ne guid'" + currentProjectUid + "'",
        method: "GET",
        success: function (response) {
            GetProjectODataSuccess(response);
        },
        error: function (responseInfo, code, statusText) {
            RequestExecutorError(responseInfo, code, statusText);
        }
    });
}

function GetProjectODataSuccess(data) {
    var document = $.parseXML(data.body);
    var feed = document.getElementsByTagName('feed')[0];
    var entryNodes = feed.getElementsByTagName('entry');
    $.each(entryNodes, function (key, entryNode) {
        var contentNode = entryNode.getElementsByTagName('content')[0];
        var propertiesNode = contentNode.getElementsByTagName('properties')[0];

        var syncObjectLinkNode = propertiesNode.getElementsByTagName(psCustomFieldSyncObjectLinkName)[0];

        var syncObjectLinkOdataArr = [];
        if (syncObjectLinkNode.innerHTML != null && syncObjectLinkNode.innerHTML !== "") {
            syncObjectLinkOdataArr = $.parseJSON(syncObjectLinkNode.innerHTML);
        }

        $.each(syncObjectLinkOdataArr, function (syncObjectLinkOdataIndex, syncObjectLinkOdata) {
            var isFound = false;

            $.each(tfsEpicsToBlock, function (tfsEpicToBlockIndex, tfsEpicToBlock) {
                if (tfsEpicToBlock.SystemId === syncObjectLinkOdata.SystemId) {
                    $.each(syncObjectLinkOdata.Epics, function (epicIndex, epic) {
                        tfsEpicToBlock.Epics.push(epic);
                    });
                    isFound = true;
                    return;
                }
            });
            if (!isFound) {
                var objToPush = {};
                objToPush['SystemId'] = syncObjectLinkOdata.SystemId;
                objToPush['Epics'] = [];
                $.each(syncObjectLinkOdata.Epics, function (epicIndex, epic) {
                    objToPush['Epics'].push(epic);
                });
                tfsEpicsToBlock.push(objToPush);
            }
        });
    });

    window.LoadPanelHide();

}




function GetTfsData() {
    var postData = {};
    postData['query'] = "SELECT * FROM WorkItems WHERE [System.WorkItemType] = 'Epic' order by [System.Title] desc";
    ExecuteTfsRequest('wit/wiql?api-version=1.0', JSON.stringify(postData), "POST", currentSystem, GetWorkItemsQuerySuccess, HandleErrorExecuteJiraRequest);

}


function GetWorkItemsQuerySuccess(response) {
    var ids = "";
    if (response.workItems.length !== 0) {
        ids = response.workItems[0].id.toString();
        $.each(response.workItems, function (key, workItem) {
            if (key !== 0) {
                ids = ids + ',' + workItem.id.toString();
            }
        });
    }
    $.when(GetFields(), GetWorkItems(ids), GetProjectsTfs()).done(GetTfsDataSuccess);
}

function GetFields() {
    return ExecuteTfsRequest('wit/fields?api-version=1.0', null, "GET", currentSystem, null, null);
}

function GetWorkItems(ids) {
    return ExecuteTfsRequest('wit/WorkItems?ids=' + ids + '&api-version=1.0', null, "GET", currentSystem, null, null);
}

function GetProjectsTfs() {
    return ExecuteTfsRequest('projects?api-version=1.0', null, "GET", currentSystem, null, null);
}

function GetTfsDataSuccess(responseFields, responseWorkItems, responseProjects) {
    var projectsTemp = responseProjects[0].value;

    jiraProjects = [];

    var objectsToBlockCurrentSystem = $.grep(objectToBlock.ProjectServerSystemLinks, function (syncObjectLink, i) {
        return syncObjectLink.SystemId === currentSystem.SystemId && syncObjectLink.IsHomeProject && syncObjectLink.ProjectKey != null;
    });

    $.each(projectsTemp, function (key, project) {
        if (objectsToBlockCurrentSystem.length === 0) {
            jiraProjects.push({ 'ProjectKey': project.name, 'ProjectId': project.name, 'ProjectName': project.name });
        }
        else {
            var projectsToBlock = $.grep(objectsToBlockCurrentSystem, function (objectToBlockCurrentSystem, i) {
                return objectToBlockCurrentSystem.ProjectId === project.name;
            });
            if (projectsToBlock.length === 0) {
                jiraProjects.push({ 'ProjectKey': project.name, 'ProjectId': project.name, 'ProjectName': project.name });
            }
        }
    });

    jiraProjectsControl.option('dataSource', new DevExpress.data.ArrayStore({
        data: jiraProjects,
        key: "ProjectId"
    }));
    jiraProjectsControl.option('displayExpr', "ProjectName");
    jiraProjectsControl.option('valueExpr', "ProjectId");

    var objectsLinkedCurrentSystem = $.grep(objectLinked.ProjectServerSystemLinks, function (syncObjectLink, i) {
        return syncObjectLink.SystemId === currentSystem.SystemId && syncObjectLink.IsHomeProject && syncObjectLink.ProjectId != null;
    });
    if (objectsLinkedCurrentSystem != null && objectsLinkedCurrentSystem.length !== 0) {
        jiraIsHomeProjectControl.option('value', objectsLinkedCurrentSystem[0].IsHomeProject);
        jiraProjectsControl.option('value', objectsLinkedCurrentSystem[0].ProjectId);
    }
    isProjectsLoaded = true;

    var tempTfsEpics = responseWorkItems;
    tfsCustomFields = responseFields;
    //var columns = tfsCustomFields[0].value;
    //$.each(columns, function (key, column) {
    //    var visible = false;
    //    if (defaultColumns.indexOf(column.referenceName) >= 0) {
    //        visible = true;
    //    }
    //    columnsTypes[column.referenceName] = column.type;
    //    allColumns.push({
    //        dataField: column.referenceName,
    //        caption: column.name,
    //        visible: visible,
    //        allowEditing: !column.readOnly,
    //        PropertyType: column.type
    //    });
    //});
    var epics = tempTfsEpics[0].value;
    $.each(epics, function (keyEpic, epic) {
        var epicToAdd = {};
        epicToAdd["System"] = { "Id": epic.id };
        epicToAdd["EpicKey"] = epic.id;
        epicToAdd["EpicName"] = epic.fields["System.Title"];
        epicToAdd["EpicId"] = epic.id;

        epicToAdd["ProjectKey"] = epic.fields["System.TeamProject"];
        epicToAdd["ProjectName"] = epic.fields["System.TeamProject"];
        epicToAdd["ProjectId"] = epic.fields["System.TeamProject"];

        //$.each(epic.fields, function (keyField, value) {
        //    var propertyPath = keyField.split('.');
        //    var resultValue;
        //    if (typeof (value) == "string") {
        //        resultValue = ConvertStringToTypeValue(value, columnsTypes[keyField]);
        //    } else {
        //        resultValue = value;
        //    }
        //    Assign(epicToAdd, propertyPath, resultValue);
        //});
        tfsEpics.push(epicToAdd);
    });
    //$.each(epics, function (keyEpic, epic) {
    //    var epicToAdd = {};
    //    epicToAdd["System"] = { "Id": epic.id };
    //    $.each(epic.fields, function (keyField, value) {
    //        var propertyPath = keyField.split('.');
    //        var resultValue;
    //        if (typeof (value) == "string") {
    //            resultValue = ConvertStringToTypeValue(value, columnsTypes[keyField]);
    //        } else {
    //            resultValue = value;
    //        }
    //        Assign(epicToAdd, propertyPath, resultValue);
    //    });
    //    tfsEpics.push(epicToAdd);
    //});

    tfsEpicsControl.option('dataSource', new DevExpress.data.ArrayStore({
        data: tfsEpics,
        key: 'System.Id'
    }));

    tfsEpicsControl.refresh();
    ToggleEdit(true);
    window.LoadPanelHide();

    //if (psDraftProject == null) {
    //    if (psCurrentProject.get_isCheckedOut()) {
    //        GetDraftProject();
    //    } else {
    //        ToggleEdit(false);
    //        window.LoadPanelHide();
    //    }
    //} else {
    //    ToggleEdit(true);
    //    window.LoadPanelHide();
    //}
}

function GetDraftProject() {
    psDraftProject = psCurrentProject.get_draft();
    psDraftProjectUser = psDraftProject.get_checkedOutBy();
    psDraftAssignments = psDraftProject.get_assignments();
    projContext.load(psDraftProject);
    projContext.load(psDraftProjectUser);;
    projContext.load(psDraftAssignments, "Include(Id, Task, Resource)");
    projContext.load(psDraftAssignments);

    projContext.executeQueryAsync(GetDraftProjectSucceeded, GetDraftProjectFailed);
}

function GetDraftProjectSucceeded(sender, args) {
    if (currentUser.get_id() === psDraftProjectUser.get_id()) {
        ToggleEditTFS(true);
        window.LoadPanelHide();
    } else {
        ToggleEditTFS(false);
        window.LoadPanelHide();
    }
}

function GetDraftProjectFailed(sender, args) {
    ToggleEditTFS(false);
    window.LoadPanelHide();
}

function ToggleEditTFS(isEditable) {
    if (!isEditable) {
        buttonPublishControl.option('disabled', true);
        jiraEpicsControl.option('selection', {
            mode: "multiple",
            showCheckBoxesMode: "none"
        });
    } else {
        buttonPublishControl.option('disabled', false);
        jiraEpicsControl.option('selection', {
            mode: "multiple",
            showCheckBoxesMode: "always"
        });
    }
}

function Assign(obj, keyPath, value) {
    var lastKeyIndex = keyPath.length - 1;
    for (var i = 0; i < lastKeyIndex; ++i) {
        var key = keyPath[i];
        if (!(key in obj))
            obj[key] = {}
        obj = obj[key];
    }
    obj[keyPath[lastKeyIndex]] = value;
}

//function GetValue(obj, keyPath) {
//    var lastKeyIndex = keyPath.length - 1;
//    for (var i = 0; i < lastKeyIndex; ++i) {
//        var key = keyPath[i];
//        if (!(key in obj))
//            obj[key] = {}
//        obj = obj[key];
//    }
//    return obj[keyPath[lastKeyIndex]];
//}

function ConvertStringToTypeValue(stringValue, destinationType) {
    switch (destinationType) {
        case "string":
        case "plainText":
            return stringValue;
        case "dateTime":
            return new Date(stringValue);
        case "integer":
            return parseInt(stringValue);
        case "double":
            return parseFloat(stringValue);
        case "boolean":
            return $.parseJSON(stringValue);
        default:
            return stringValue;
    }
}

function PublishClick(data) {
    buttonPublishControl.option('disabled', true);

    if (originalSyncObjectLink != null) {
        newSyncObjectLinkArray = $.parseJSON(originalSyncObjectLink);
    }

    var isFound = false;

    $.each(newSyncObjectLinkArray, function (key, syncObjectLink) {
        if (syncObjectLink.SystemId === currentSystem.SystemId) {
            syncObjectLink.Epics = jiraEpicsControl.getSelectedRowKeys();
            isFound = true;
            return;
        }
    });

    if (!isFound) {
        var syncObjectLink = {};
        syncObjectLink['SystemId'] = currentSystem.SystemId;
        syncObjectLink['Epics'] = jiraEpicsControl.getSelectedRowKeys();
        newSyncObjectLinkArray.push(syncObjectLink);
    }
    psDraftProject.set_item(psCustomFieldSyncObjectLink.get_internalName(), JSON.stringify(newSyncObjectLinkArray));

    UpdateJob();
}

function UpdateJob() {
    window.LoadPanelShow();
    window.updateJob = psDraftProject.update(true);
    projContext.waitForQueueAsync(window.updateJob, queueTimeOut, UpdateJobSent);
}

function UpdateJobSent(response) {
    if (response === 4 && updateJob.get_jobState() === 4) {
        window.LoadPanelShow();
        PublishJob();
    }
    else {
        updateJob.cancel();
        projContext.waitForQueueAsync(updateJob, queueTimeOut, UpdateJobCancelSent);
    }
}

function UpdateJobCancelSent(response) {
    RollbackChanges();
}

function PublishJob() {
    window.publishJob = psDraftProject.publish(false);
    projContext.waitForQueueAsync(window.publishJob, queueTimeOut, PublishJobSent);
}

function PublishJobSent(response) {
    if (response === 4 && publishJob.get_jobState() === 4) {
        window.LoadPanelHide();
        appClientContext.executeQueryAsync(QuerySucceeded, QueryFailed);
    } else {
        publishJob.cancel();
        projContext.waitForQueueAsync(publishJob, queueTimeOut, PublishJobCancelSent);
    }
}

function PublishJobCancelSent(response) {
    RollbackChanges();
}

function RollbackChanges() {
    window.LoadPanelHide();
    alert("Publish failed. Please try to check-in and then check-out project. If it will not help contact your system administrator");
    buttonPublishControl.option('disabled', false);
}

function QuerySucceeded(sender, args) {
    originalSyncObjectLink = psCurrentProject.get_includeCustomFields().get_fieldValues()[psCustomFieldSyncObjectLink.get_internalName()];

    selectedTfsEpics = [];

    $.each(newSyncObjectLinkArray, function (key, syncObjectLink) {
        if (syncObjectLink.SystemId === currentSystem.SystemId) {
            var epicsObject = {};
            epicsObject['SystemId'] = currentSystem.SystemId;
            epicsObject['Epics'] = [];
            $.each(syncObjectLink.Epics, function (keyE, epicKey) {
                epicsObject.Epics.push(epicKey);
            });
            selectedTfsEpics.push(epicsObject);
            return;
        }
    });
    SyncAllClick();
    window.LoadPanelHide();
    buttonPublishControl.option('disabled', false);
}

function QueryFailed(sender, args) {
    console.log("Publish query failed: " + args.get_message());
    alert("Publish query failed.");
    buttonPublishControl.option('disabled', false);
}