// Install the angularjs.TypeScript.DefinitelyTyped NuGet package
var UICodeMirror;
(function (UICodeMirror) {
    "use strict";
    UICodeMirrorDirective.$inject = ["$window"];
    function UICodeMirrorDirective($window) {
        return {
            restrict: 'EA',
            require: '?ngModel',
            scope: {
                model: '=ngModel',
                options: '=uiCodemirrorOptions'
            },
            link: link
        };
        function link(scope, element, attrs) {
            // Require CodeMirror
            if (angular.isUndefined(window.CodeMirror)) {
                throw new Error('ui-codemirror needs CodeMirror to work... (o rly?)');
            }
            var codemirrorOptions = angular.extend({ value: scope.model }, scope.options || {});
            var codemirror = newCodemirrorEditor(element, codemirrorOptions);
            // Apply watchers
            watchOptionsForChange(codemirror, scope);
            watchCodemirrorForChange(codemirror, scope);
            // Allow access to the CodeMirror instance through a broadcasted event
            // eg: $broadcast('CodeMirror', function(cm){...});
            scope.$on('CodeMirror', function (event, callback) {
                if (angular.isFunction(callback)) {
                    callback(codemirror);
                }
                else {
                    throw new Error('the CodeMirror event requires a callback function');
                }
            });
            // onLoad callback
            if (angular.isFunction(codemirrorOptions.onLoad)) {
                codemirrorOptions.onLoad(codemirror);
            }
        }
        function newCodemirrorEditor(element, codemirrorOptions) {
            var codemirror;
            if (element[0].tagName === 'TEXTAREA') {
                // Might bug but still ...
                codemirror = CodeMirror.fromTextArea(element[0], codemirrorOptions);
            }
            else {
                element.html('');
                codemirror = CodeMirror(function (cm_el) {
                    element.append(cm_el);
                }, codemirrorOptions);
            }
            return codemirror;
        }
        function watchOptionsForChange(codemirror, scope) {
            var codemirrorDefaultsKeys = Object.keys(CodeMirror.defaults);
            scope.$watch('options', function (newValues, oldValues, scope) {
                if (!angular.isObject(newValues)) {
                    return;
                }
                codemirrorDefaultsKeys.forEach(function (key) {
                    if (newValues.hasOwnProperty(key)) {
                        if (oldValues && newValues[key] === oldValues[key]) {
                            return;
                        }
                        codemirror.setOption(key, newValues[key]);
                    }
                });
            }, true);
        }
        function watchCodemirrorForChange(codemirror, scope) {
            var doc = codemirror.getDoc();
            scope.$watch('model', function (newValue, oldValue) {
                //Code mirror expects a string so make sure it gets one
                //Although the formatter have already done this, it can be possible that another formatter returns undefined (for example the required directive)
                var docValue = doc.getValue();
                if (docValue !== newValue) {
                    var safeValue = newValue || '';
                    doc.setValue(safeValue);
                }
            });
            codemirror.on('change', function (instance, change) {
                var doc = instance.getDoc();
                var newValue = doc.getValue();
                if (newValue !== scope.model) {
                    scope.model = newValue;
                    scope.$apply();
                }
            });
        }
    }
    var app = angular.module("ui.codemirror", []);
    app.directive("uiCodemirror", UICodeMirrorDirective);
})(UICodeMirror || (UICodeMirror = {}));
//# sourceMappingURL=UICodeMirrorDirective.js.map