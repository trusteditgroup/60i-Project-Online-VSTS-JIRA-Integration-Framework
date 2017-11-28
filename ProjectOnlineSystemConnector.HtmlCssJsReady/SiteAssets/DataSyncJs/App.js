'use strict';

var updateJob, publishJob, checkInJob;
var queueTimeOut = 12000;

var currentProjectUid;
var currentUser;
var clientContext, projContext, appClientContext;
var appWeb;
var appWebUrl;
var loadPanelControl;
var requestExecutor, requestExecutorHost;
var formDigestValue;
var guidEmpty = "00000000-0000-0000-0000-000000000000";

var syncSettingsItems;
var systemProxyUrl = "";
var signalRPort = "1988";
var signalRUrl = "http://localhost:1988";

var psListFieldSystemProxyUrl = "SystemProxyUrl";
var psListFieldSignalRPort = "SignalRPort";

// This code runs when the DOM is ready and creates a context object which is needed to use the SharePoint object model
function InitApplication() {

    loadPanelControl = $("#LoadPanel").dxLoadPanel({
        shadingColor: "rgba(0,0,0,0.4)",
        visible: false,
        showIndicator: true,
        showPane: false,
        shading: true,
        closeOnOutsideClick: false
    }).dxLoadPanel("instance");

    LoadPanelShow();
    $.ajaxSetup({ cache: false });
    $(document)
        .ajaxStart(function (event) {
            var a = 1;
        })
        .ajaxStop(function (event) {
            var a = 1;
        })
        .ajaxComplete(function (event, jqXhr, settings) {
            var a = 1;
        })
        .ajaxError(function (event, jqXhr, settings) {
            LoadPanelHide();
        });

    projContext = PS.ProjectContext.get_current();
    clientContext = SP.ClientContext.get_current();

    //appWebUrl = window.location.origin + projContext.get_url();
    appWebUrl = window.location.protocol + "//" + window.location.host + projContext.get_url();

    appClientContext = new SP.ClientContext(appWebUrl);
    var factory = new SP.ProxyWebRequestExecutorFactory(appWebUrl);
    appClientContext.set_webRequestExecutorFactory(factory);
    appWeb = projContext.get_web();
    requestExecutor = new SP.RequestExecutor(appWebUrl);
}

function GetNodeValue(propertiesNode, propertyName) {
    var value = "";
    var node = propertiesNode.find(propertyName);
    if (node.length === 0) {
        var searchString1 = 'd\\:' + propertyName;
        node = propertiesNode.find(searchString1);
    }
    if (node.length !== 0) {
        value = node.text();
    }
    return value;
}

function LoadSignalRScript(getSyncSettingsSuccess) {
    $.getScript(signalRUrl + "/signalr/hubs")
        .done(function( script, textStatus ) {
            getSyncSettingsSuccess();
        })
        .fail(function( jqxhr, settings, exception ) {
            getSyncSettingsSuccess();
        });
}

function SetUpSignalRHub(onGetLogMessage, projectUid) {
    //Set the hubs URL for the connection
    $.connection.hub.url = signalRUrl + "/signalr";
    // Declare a proxy to reference the hub.
    var webHookDataProcessorHub = $.connection.webHookDataProcessorHub;
    if (webHookDataProcessorHub == null || webHookDataProcessorHub === 'undefined') {
        return;
    }
    // Create a function that the hub can call to broadcast messages.
    webHookDataProcessorHub.client.GetLogMessage = function (message) {
        //debugger;
        //var a = message;
        if (onGetLogMessage != null) {
            onGetLogMessage(message);
        }
    };
    $.connection.hub.start().done(function () {
        webHookDataProcessorHub.server.subscribe(projectUid);
    });
}

function GetSyncSettings(getSyncSettingsSuccess) {
    requestExecutor.executeAsync({
        url: appWebUrl + "/_vti_bin/listdata.svc/SyncSettings",
        method: "GET",
        success: function (data) {
            var xmlDoc = $.parseXML(data.body);
            var entryNodes = $(xmlDoc).find('feed entry');

            $.each(entryNodes, function (key, entryNode) {
                var contentNode = $(entryNode).find('content');
                var propertiesNode = contentNode.find('properties');
                if (propertiesNode.length === 0) {
                    propertiesNode = contentNode.find('m\\:properties');
                }
                if (propertiesNode.length !== 0) {
                    systemProxyUrl = GetNodeValue(propertiesNode, psListFieldSystemProxyUrl);
                    signalRPort = GetNodeValue(propertiesNode, psListFieldSignalRPort);
                }
                if (systemProxyUrl.charAt(systemProxyUrl.length - 1) === "/") {
                    systemProxyUrl = systemProxyUrl.substring(0, systemProxyUrl.length - 1);
                }
                signalRUrl = systemProxyUrl + ":" + signalRPort;
                systemProxyUrl += "/";
            });
            //LoadSignalRScript(getSyncSettingsSuccess);
            getSyncSettingsSuccess();
        },
        error: function (responseInfo, code, statusText) {
            RequestExecutorError(responseInfo, code, statusText);
        }
    });
}

function GetCurrentUser(getCurrentUserSucceeded, getCurrentUserFailed) {
    currentUser = appWeb.get_currentUser();
    projContext.load(currentUser);
    projContext.executeQueryAsync(getCurrentUserSucceeded, getCurrentUserFailed);
}

function ExecuteQueryAsyncFail(sender, args) {
    window.LoadPanelHide();
    alert('Project Online. Query Failed. Error:' + args.get_message());
}

function LoadPanelShow() {
    loadPanelControl.show();
}

function LoadPanelHide() {
    loadPanelControl.hide();
}

function GetQueryStringParameter(paramToRetrieve) {
    paramToRetrieve = paramToRetrieve.toLowerCase();
    var params = document.URL.toLowerCase().split("?")[1].split("&");
    var strParams = "";
    for (var i = 0; i < params.length; i = i + 1) {
        var singleParam = params[i].split("=");
        if (singleParam[0] === paramToRetrieve)
            strParams = decodeURIComponent(singleParam[1]);
    }
    return strParams;
}

function RequestExecutorError(responseInfo, code, statusText) {
    window.LoadPanelHide();
    var errorMessage = "";
    if (responseInfo.body.indexOf('xml')<0) {
        var errorJsonBody = $.parseJSON(responseInfo.body);
        errorMessage = errorJsonBody.error.message.value;
    } else {
        errorMessage = $($.parseXML(responseInfo.body)).children().find('message').text();

    }
    alert(errorMessage);
}

function ExecuteAsync(url, method, contentType, postJson, executeAsyncSuccess, executeAsyncError) {
    requestExecutor.executeAsync({
        url: appWebUrl + url,
        method: method,
        body: postJson,
        headers: {
            "accept": "application/" + contentType + ";odata=verbose",
            "content-type": "application/" + contentType + ";odata=verbose",
            "X-RequestDigest": formDigestValue
        },
        success: function (response) {
            executeAsyncSuccess(response);
        },
        error: function (responseInfo, code, statusText) {
            executeAsyncError(responseInfo, code, statusText);
        }
    });
}

function GetFormDigestValue(getFormDigestValueSucceeded) {
    requestExecutor.executeAsync({
        url: appWebUrl + "/_api/contextinfo",
        method: "POST",
        headers: {
            "accept": "application/json;odata=verbose",
            "content-type": "application/json;odata=verbose",
            "X-RequestDigest": formDigestValue
        },
        success: function (response) {
            GetFormDigestValueSucceededBase(response);
            getFormDigestValueSucceeded();
        },
        error: function (responseInfo, code, statusText) {
            RequestExecutorError(responseInfo, code, statusText);
        }
    });
}

function GetFormDigestValueSucceededBase(response) {
    var responseObj = $.parseJSON(response.body);
    formDigestValue = responseObj.d.GetContextWebInformation.FormDigestValue;
}

function GetCurrentProjectUid(getCurrentProjectUidSucceeded) {
    currentProjectUid = GetQueryStringParameter("ProjUid");
    getCurrentProjectUidSucceeded();
}

$.urlParam = function (name) {
    var results = new RegExp('[\?&]' + name + '=([^&#]*)').exec(window.location.href);
    if (results == null) {
        return null;
    }
    else {
        return decodeURI(results[1]) || 0;
    }
}

//This prototype function allows you to remove even array from array
Array.prototype.remove = function (x) {
    var i;
    var array = this;
    for (i in array) {
        if (array.hasOwnProperty(i)) {
            if (array[i].toString() === x.toString()) {
                array.splice(i, 1);
            }
        }
    }
}

function CommonAjaxRequest(url, queryParameters, method, dataObject, successFunction, errorFunction, dataType) {
    if (queryParameters != null) {
        url = url + "?" + $.param(queryParameters);
    }
    if (dataType == null || dataType === "") {
        dataType = "json";
    }
    return $.ajax({
        url: url,
        method: method,
        //dataType: dataType,
        async: true,
        //contentType: "application/json; charset=utf-8",
        data: dataObject,
        //beforeSend: function (xhr) {
        //    xhr.setRequestHeader("Access-Control-Allow-Origin", "*");
        //    xhr.setRequestHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS, PUT, DELETE");
        //},
        success: function (result) {
            if (successFunction != null) {
                successFunction(result);
            }
        },
        error: function (jqXhr, textStatus, error) {
            if (errorFunction != null) {
                errorFunction(jqXhr, textStatus, error);
            }
        }
    });
}

function CommonAjaxError(jqXhr, textStatus, error, deferred) {
    debugger;
    LoadPanelHide();
    if (deferred != null) {
        deferred.reject();
    }
    alert(error + ". " + jqXhr.responseJSON.Exception.Message);
}