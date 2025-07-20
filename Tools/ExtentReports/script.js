// Uncomment for local tests and disable browser file CORS security, avoid to drag & drop the file each time
/*var request = new XMLHttpRequest();
//request.open("GET", "./playmode-results-all-ok.xml", true);
request.open("GET", "./playmode-results-some-failed.xml", true);
request.onreadystatechange = function () {
    if (request.readyState == 4) {
        if (request.status == 200) {
            ProcessXMLDocument(request.responseXML);
        }
    }
}
request.send();*/

const report = document.getElementById("report");
report.hidden = true;

const fileInput = document.getElementById("fileInput");
function clicHandler(ev) {
    if(document.createEvent) {
        var evt = document.createEvent("MouseEvents");
        evt.initEvent("click", true, false);
        fileInput.dispatchEvent(evt);
    }
}

function fileChangeHandler(ev) {
    if (ev.target.files[0]) {
        ProcessFile(ev.target.files[0]);
    }
}

function dragOverHandler(ev) {
    // Prevent default behavior (Prevent file from being opened)
    ev.preventDefault();
}

function dropHandler(ev) {
    // Prevent default behavior (Prevent file from being opened)
    ev.preventDefault();

    if (ev.dataTransfer.items) {
        // Use DataTransferItemList interface to access the file(s)
        [...ev.dataTransfer.items].forEach((item, i) => {
            // If dropped items aren't files, reject them
            if (item.kind === "file") {
                const file = item.getAsFile();
                ProcessFile(file);
            }
        });
    } else {
        // Use DataTransfer interface to access the file(s)
        [...ev.dataTransfer.files].forEach((file, i) => {
            console.log(`… file[${i}].name = ${file.name}`);
        });
    }
}

function ProcessFile(file) {
    if (file.type === "text/xml") {
        var reader = new FileReader();
        reader.readAsText(file, "UTF-8");
        reader.onload = function (evt) {
            const parser = new DOMParser();
            const xmlDocument = parser.parseFromString(evt.target.result, file.type);
            ProcessXMLDocument(xmlDocument);
        }
        reader.onerror = function (evt) {
            document.getElementById("fileContents").innerHTML = "error reading file";
        }
    }
}

function ProcessXMLDocument(xmlDocument) {
    const testCases = xmlDocument.getElementsByTagName("test-case");
    var passsedTestCases = xmlDocument.createDocumentFragment();
    var failedTestCases = xmlDocument.createDocumentFragment();
    var skippedTestCases = xmlDocument.createDocumentFragment();
    for (var i = 0; i < testCases.length; i++) {
        if (testCases[i].attributes["result"].value === "Passed") {
            passsedTestCases.appendChild(testCases[i].cloneNode(true));
        } else if (testCases[i].attributes["result"].value === "Failed") {
            failedTestCases.appendChild(testCases[i].cloneNode(true));
        } else if (testCases[i].attributes["result"].value === "Skipped") {
            skippedTestCases.appendChild(testCases[i].cloneNode(true));
        }
    }
    report.hidden = false;
    document.getElementById("test-cases").textContent = '';
    ProcessSummary(xmlDocument);
    ProcessFailedTests(failedTestCases);
    ProcessSkippedTests(skippedTestCases);
    ProcessPassedTests(passsedTestCases);
}

function ProcessSummary(xmlDocument) {
    document.getElementById("total-count").innerText = "Tests: " + xmlDocument.firstChild.attributes["testcasecount"].value;
    document.getElementById("failed-count").innerText = "Failed: " + xmlDocument.firstChild.attributes["failed"].value;
    document.getElementById("passed-count").innerText = "Passed:" + xmlDocument.firstChild.attributes["passed"].value;
    document.getElementById("inconclusive-count").innerText = "Inconclusive: " + xmlDocument.firstChild.attributes["inconclusive"].value;
    document.getElementById("skipped-count").innerText = "Skipped: " + xmlDocument.firstChild.attributes["skipped"].value;
    document.getElementById("date").innerText = "Date: " + xmlDocument.firstChild.attributes["start-time"].value;
    document.getElementById("duration").innerText = "Duration: " + xmlDocument.firstChild.attributes["duration"].value;
}

function ProcessFailedTests(failedTestCases) {
    const template = document.getElementById("template-failed");
    const root = document.getElementById("test-cases");
    for (var i = 0; i < failedTestCases.childNodes.length; i++) {
        const testCase = failedTestCases.childNodes[i];
        let testCaseHtml = template.content.cloneNode(true);
        const elements = testCaseHtml.children[0].children;
        elements[0].textContent = testCase.attributes["fullname"].value;
        elements[1].childNodes[3].textContent = "Duration: " + testCase.attributes["duration"].value;
        elements[3].textContent = testCase.getElementsByTagName("failure")[0].getElementsByTagName("message")[0].textContent;

        const output = testCase.getElementsByTagName("output")[0];
        if (output !== undefined) {
            elements[7].textContent = output.textContent;
        } else {
            testCaseHtml.children[0].removeChild(elements[7]);
            testCaseHtml.children[0].removeChild(elements[6]);
        }

        const stackTrace = testCase.getElementsByTagName("failure")[0].getElementsByTagName("stack-trace")[0];
        if (stackTrace !== undefined) {
            elements[5].textContent = stackTrace.textContent;
        } else {
            testCaseHtml.children[0].removeChild(elements[5]);
            testCaseHtml.children[0].removeChild(elements[4]);
        }
        root.appendChild(testCaseHtml);
    }
}

function ProcessSkippedTests(skippedTestCases) {
    const template = document.getElementById("template-skipped");
    const root = document.getElementById("test-cases");
    for (var i = 0; i < skippedTestCases.childNodes.length; i++) {
        const testCase = skippedTestCases.childNodes[i];
        let testCaseHtml = template.content.cloneNode(true);
        const elements = testCaseHtml.children[0].children;
        elements[0].textContent = testCase.attributes["fullname"].value;
        elements[1].childNodes[3].textContent = "Duration: " + testCase.attributes["duration"].value;
        const reason = testCase.getElementsByTagName("reason")[0].getElementsByTagName("message")[0];
        if (reason !== undefined) {
            elements[3].textContent = reason.textContent;
        } else {
            testCaseHtml.children[0].removeChild(elements[3]);
            testCaseHtml.children[0].removeChild(elements[2]);
        }
        root.appendChild(testCaseHtml);
    }
}

function ProcessPassedTests(passsedTestCases) {
    const template = document.getElementById("template-passed");
    const root = document.getElementById("test-cases");
    for (var i = 0; i < passsedTestCases.childNodes.length; i++) {
        const testCase = passsedTestCases.childNodes[i];
        let testCaseHtml = template.content.cloneNode(true);
        const elements = testCaseHtml.children[0].children;
        elements[0].textContent = testCase.attributes["fullname"].value;
        elements[1].childNodes[3].textContent = "Duration: " + testCase.attributes["duration"].value;

        const output = testCase.getElementsByTagName("output")[0];
        if (output !== undefined) {
            elements[3].textContent = output.textContent;
        } else {
            testCaseHtml.children[0].removeChild(elements[3]);
            testCaseHtml.children[0].removeChild(elements[2]);
        }
        root.appendChild(testCaseHtml);
    }
}
