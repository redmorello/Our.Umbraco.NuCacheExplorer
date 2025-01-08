var app = angular.module("umbraco");

app.requires.push("ui.codemirror");

app.controller("OurUmbracoNuCacheExplorer.Dashboard", function ($scope, $http) {

    var vm = this;

    vm.load = load;
    vm.data = null;
    vm.totalItems = 0;
    vm.clock = null;
    vm.editor = null;
    vm.themes = [];
    vm.selectedTheme = 'default';
    vm.loaded = false;

    vm.editorOptions = {
        tabSize: 4,
        lineNumbers: true,
        line: true,
        foldGutter: true,
        gutters: ['CodeMirror-linenumbers', 'CodeMirror-foldgutter'],
        mode: 'application/json',
        matchBrackets: true,
        showCursorWhenSelecting: true,
        theme: vm.selectedTheme,
        readOnly: true,
        keyMap: 'basic',
        viewportMargin: 10,
        extraKeys: { "Ctrl-F": "findPersistent", "Ctrl-G": "findNext", "Shift-Ctrl-G": "findPrev" }
    };

    function load(contentType) {
        if (vm.working) return;
        vm.working = true;
        vm.loaded = false;
        vm.data = "(loading...)";
        $http.get("backoffice/OurUmbracoNuCacheExplorer/NuCacheExplorerApi/GetNuCacheFile?contentType=" + contentType)
            .then(function success(response) {
                vm.data = response.data.Items;
                vm.totalItems = response.data.TotalItems;
                vm.clock = response.data.StopClock;
                vm.data = JSON.stringify(response.data.Items, null, 4);

                vm.loaded = true;
                vm.working = false;
            }, function error(response) {
                vm.data = response.data.Message;
                vm.working = false;
            }
            );

        event.preventDefault();
        event.stopPropagation();
    }

    function init() {
        vm.working = false;
    }

    init();
});