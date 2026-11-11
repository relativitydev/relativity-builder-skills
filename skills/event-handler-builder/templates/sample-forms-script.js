// Relativity Forms event handler script. Upload this file as a resource file on the same
// application as the Page Interaction event handler DLL, using exactly the file name returned
// by ScriptFileNames.
//
// Write ES5.1 (no arrow functions, let/const, classes, or template literals). The file is a
// single immediately invoked function that receives eventNames and convenienceApi and returns a
// map of event name -> handler. See the Relativity Forms API reference for every event and helper.
(function (eventNames, convenienceApi) {
    var eventHandlers = {};

    // Runs after everything else in the load pipeline has finished.
    eventHandlers[eventNames.PAGE_LOAD_COMPLETE] = function () {
        console.log("MyApp: Sample Object form loaded.");
    };

    // Adds a button to the console panel. Runs for View mode layouts only, on full page loads.
    // This replaces the classic ConsoleEventHandler, which Relativity Forms ignores.
    eventHandlers[eventNames.CREATE_CONSOLE] = function () {
        var button = document.createElement("button");
        button.id = "myAppHelpButton";
        button.textContent = "Open help";
        button.onclick = function () {
            window.open("https://example.com/help", "_blank", "noopener");
        };

        return convenienceApi.console.containersPromise.then(function (containers) {
            containers.rootElement.appendChild(button);
        });
    };

    return eventHandlers;
}(eventNames, convenienceApi));
