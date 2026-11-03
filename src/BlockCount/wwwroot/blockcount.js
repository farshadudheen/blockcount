(function () {
    "use strict";

    var blockListEditorAlias = "Umbraco.BlockList";
    var blockGridEditorAlias = "Umbraco.BlockGrid";

    var countPanel =
        '<div class="umb-group-panel" data-block-count>' +
            '<div class="umb-group-panel__header">' +
                '<localize key="blockCount_count">Count</localize>' +
            "</div>" +
            '<div class="umb-group-panel__content">' +
                '<div class="control-group umb-control-group -no-border">' +
                    '<div class="umb-el-wrap">' +
                        '<label class="control-label">' +
                            '<localize key="blockCount_amount">Amount</localize>' +
                        "</label>" +
                        '<div class="controls">' +
                            "<block-count-amount></block-count-amount>" +
                            '<p class="help">' +
                                '<localize key="blockCount_amountDescription">Minimum and maximum number of this block type. Leave either value empty for no limit. Set both to the same number for an exact count.</localize>' +
                            "</p>" +
                        "</div>" +
                    "</div>" +
                "</div>" +
            "</div>" +
        "</div>";

    var validationMarkup =
        '<div data-block-count-validation>' +
            '<input type="hidden" name="blockCount" ng-model="vm.layout" />' +
            '<div class="help text-error" ng-repeat="message in vm.blockCountMessages track by $index" ng-bind="message"></div>' +
        "</div>";

    angular.module("umbraco").config(["$httpProvider", "$provide", function ($httpProvider, $provide) {
        $httpProvider.interceptors.push("blockCountTemplateInterceptor");

        $provide.decorator("umbBlockListPropertyEditorDirective", ["$delegate", function ($delegate) {
            decorateEditor($delegate, function ($scope) {
                return $scope;
            }, blockListEditorAlias, [
                "$scope", "$timeout", "$interpolate", "editorService", "clipboardService", "localizationService",
                "overlayService", "blockEditorService", "udiService", "serverValidationManager", "angularHelper",
                "eventsService", "$attrs"
            ]);
            return $delegate;
        }]);

        $provide.decorator("umbBlockGridPropertyEditorDirective", ["$delegate", function ($delegate) {
            decorateEditor($delegate, function ($element, $attrs, $scope) {
                return $scope;
            }, blockGridEditorAlias, [
                "$element", "$attrs", "$scope", "$timeout", "$q", "editorService", "clipboardService",
                "localizationService", "overlayService", "blockEditorService", "udiService", "serverValidationManager",
                "angularHelper", "eventsService", "assetsService", "umbRequestHelper"
            ]);
            return $delegate;
        }]);
    }]);

    angular.module("umbraco").factory("blockCountTemplateInterceptor", function () {
        return {
            response: function (response) {
                var url = response.config && response.config.url || "";
                if (typeof response.data !== "string" || response.data.indexOf("data-block-count") !== -1) {
                    return response;
                }

                if (url.indexOf("blocklist.blockconfiguration.overlay.html") !== -1) {
                    response.data = insertListPanel(response.data);
                } else if (url.indexOf("blockgrid.blockconfiguration.overlay.html") !== -1) {
                    response.data = insertGridTab(response.data);
                } else if (url.indexOf("umb-block-list-property-editor.html") !== -1 || url.indexOf("umb-block-grid-property-editor.html") !== -1) {
                    response.data = insertValidation(response.data);
                }

                return response;
            }
        };
    });

    angular.module("umbraco").directive("blockCountAmount", function () {
        return {
            restrict: "E",
            controller: ["$scope", function ($scope) {
                var block = $scope.vm && $scope.vm.block;
                if (!block) {
                    return;
                }

                if (!block.blockCount || typeof block.blockCount !== "object") {
                    block.blockCount = {};
                }

                $scope.blockCountModel = {
                    alias: "blockCount",
                    hideLabel: true,
                    view: "numberrange",
                    config: {},
                    value: block.blockCount
                };
            }],
            template: '<umb-property-editor model="blockCountModel" is-pre-value="true"></umb-property-editor>'
        };
    });

    angular.module("umbraco").directive("blockCountGridTab", ["localizationService", function (localizationService) {
        return {
            restrict: "E",
            link: function (scope) {
                var vm = scope.vm;
                if (!vm) {
                    return;
                }

                function ensure() {
                    var navigation = vm.navigation;
                    if (!angular.isArray(navigation) || navigation.length === 0) {
                        return;
                    }

                    if (navigation.some(function (item) { return item.alias === "count"; })) {
                        return;
                    }

                    navigation.push({
                        alias: "count",
                        name: "Count",
                        icon: "icon-list",
                        view: "",
                        active: false
                    });

                    localizationService.localize("blockCount_count").then(function (name) {
                        var item = (vm.navigation || []).find(function (entry) { return entry.alias === "count"; });
                        if (item && name && name.indexOf("blockCount_") === -1) {
                            item.name = name;
                        }
                    });
                }

                scope.$watch(function () { return vm.navigation; }, ensure);
                scope.$watchCollection(function () { return vm.navigation; }, ensure);
            }
        };
    }]);

    function decorateEditor(directives, scopeFrom, editorAlias, inject) {
        var directive = directives[0];
        var original = directive.controller;
        var controller = function () {
            original.apply(this, arguments);
            var args = Array.prototype.slice.call(arguments);
            var $scope = scopeFrom.apply(null, args);
            attachValidation(this, $scope, editorAlias);
        };
        controller.$inject = inject;
        directive.controller = controller;
    }

    function attachValidation(vm, $scope, editorAlias) {
        var originalInit = vm.$onInit;
        vm.$onInit = function () {
            if (angular.isFunction(originalInit)) {
                originalInit.apply(vm, arguments);
            }

            $scope.$watch(function () {
                return snapshot(vm);
            }, function () {
                applyValidation(vm, editorAlias);
            });
        };
    }

    function insertListPanel(html) {
        var container = html.lastIndexOf("</umb-editor-container>");
        if (container < 0) {
            return html;
        }

        var formEnd = html.lastIndexOf("</div>", container);
        if (formEnd < 0) {
            return html;
        }

        return html.slice(0, formEnd) + countPanel + html.slice(formEnd);
    }

    function insertGridTab(html) {
        var container = html.lastIndexOf("</umb-editor-container>");
        if (container < 0) {
            return html;
        }

        var pane = '<div ng-if="vm.activeTab.alias === \'count\'" class="form-vertical">' + countPanel + "</div><block-count-grid-tab></block-count-grid-tab>";
        return html.slice(0, container) + pane + html.slice(container);
    }

    function insertValidation(html) {
        if (html.indexOf("data-block-count-validation") !== -1) {
            return html;
        }

        var end = html.lastIndexOf("</div>");
        if (end < 0) {
            return html;
        }

        return html.slice(0, end) + validationMarkup + html.slice(end);
    }

    function applyValidation(vm, editorAlias) {
        if (!vm.model) {
            return;
        }

        var issues = evaluateBlockCount(vm.model.value, readRules(vm.model.config), namesFrom(vm), editorAlias);
        vm.blockCountMessages = issues.map(function (issue) { return issue.message; });
        var control = vm.propertyForm && vm.propertyForm.blockCount;
        if (control) {
            control.$setValidity("blockCount", issues.length === 0);
        }
    }

    function snapshot(vm) {
        var value = vm.model && vm.model.value;
        var blocks = vm.model && vm.model.config && vm.model.config.blocks;
        return [
            contentSignature(value),
            layoutSignature(value && value.layout),
            blocksSignature(blocks),
            (vm.availableBlockTypes || []).length,
            vm.propertyForm && vm.propertyForm.blockCount ? 1 : 0
        ].join("|");
    }

    function contentSignature(value) {
        var entries = value && value.contentData || [];
        if (!angular.isArray(entries)) {
            return "";
        }

        return entries.map(function (entry) {
            return blockKey(entry && (entry.key || entry.udi)) + "=" + (entry && entry.contentTypeKey || "");
        }).join(";");
    }

    function layoutSignature(layout) {
        if (!layout || typeof layout !== "object") {
            return "";
        }

        return Object.keys(layout).map(function (alias) {
            return alias + ":" + itemsSignature(layout[alias]);
        }).join("|");
    }

    function itemsSignature(items) {
        if (!angular.isArray(items)) {
            return "";
        }

        return items.map(function (item) {
            var areas = item && item.areas;
            var areaPart = angular.isArray(areas)
                ? areas.map(function (area) { return itemsSignature(area && area.items); }).join(",")
                : "";
            return blockKey(item && (item.contentKey || item.contentUdi)) + "(" + areaPart + ")";
        }).join(";");
    }

    function blocksSignature(blocks) {
        if (!angular.isArray(blocks)) {
            return "";
        }

        return blocks.map(function (block) {
            var count = block && block.blockCount || {};
            return (block && block.contentElementTypeKey || "") + ":" + count.min + ":" + count.max;
        }).join(";");
    }

    function namesFrom(vm) {
        var names = {};
        (vm.availableBlockTypes || []).forEach(function (entry) {
            var config = entry && (entry.blockConfigModel || entry);
            var element = entry && entry.elementTypeModel;
            var key = config && config.contentElementTypeKey;
            var name = element && element.name;
            if (key && name) {
                names[normalizeKey(key)] = name;
            }
        });
        return names;
    }

    function readRules(config) {
        var blocks = config && config.blocks;
        if (!angular.isArray(blocks)) {
            return [];
        }

        return blocks.filter(function (block) {
            return block && block.contentElementTypeKey;
        }).map(function (block) {
            var count = block.blockCount || {};
            return {
                contentElementTypeKey: block.contentElementTypeKey,
                min: count.min,
                max: count.max
            };
        });
    }

    function entryLabel(count) {
        return count === 1 ? "entry" : "entries";
    }

    function belowMinimumMessage(name, minimum, count) {
        return name + " requires at least " + minimum + " " + entryLabel(minimum) + ". The current count is " + count + ".";
    }

    function aboveMaximumMessage(name, maximum, count) {
        return name + " allows at most " + maximum + " " + entryLabel(maximum) + ". The current count is " + count + ".";
    }

    function exactMessage(name, expected, count) {
        return name + " must contain exactly " + expected + " " + entryLabel(expected) + ". The current count is " + count + ".";
    }

    function minimumAboveMaximumMessage(name) {
        return "The count configuration for " + name + " is invalid. The minimum must be less than or equal to the maximum.";
    }

    function negativeMessage(name) {
        return "The count configuration for " + name + " is invalid. Minimum and maximum must be zero or greater.";
    }

    function evaluateBlockCount(value, rules, names, editorAlias) {
        var counts = countLayoutItems(value, editorAlias);
        var issues = [];
        var seen = {};

        (rules || []).forEach(function (rule) {
            var key = normalizeKey(rule.contentElementTypeKey);
            if (!key || seen[key]) {
                return;
            }

            seen[key] = true;
            var name = (names && (names[key] || names[rule.contentElementTypeKey])) || "Block";
            var min = normalizeLimit(rule.min);
            var max = normalizeLimit(rule.max);

            if ((min !== undefined && min < 0) || (max !== undefined && max < 0)) {
                issues.push(negativeMessage(name));
                return;
            }

            if (min !== undefined && max !== undefined && min > max) {
                issues.push(minimumAboveMaximumMessage(name));
                return;
            }

            if (min === undefined && max === undefined) {
                return;
            }

            var count = counts[key] || 0;
            if (min !== undefined && max === min && count !== min) {
                issues.push(exactMessage(name, min, count));
                return;
            }

            if (min !== undefined && count < min) {
                issues.push(belowMinimumMessage(name, min, count));
            }

            if (max !== undefined && count > max) {
                issues.push(aboveMaximumMessage(name, max, count));
            }
        });

        return issues.map(function (message) { return { message: message }; });
    }

    function countLayoutItems(value, editorAlias) {
        var counts = {};
        var contentTypes = {};
        var entries = value && value.contentData || [];
        if (angular.isArray(entries)) {
            entries.forEach(function (entry) {
                var entryKey = blockKey(entry && (entry.key || entry.udi));
                if (!entry || !entryKey || !entry.contentTypeKey) {
                    return;
                }

                contentTypes[entryKey] = normalizeKey(entry.contentTypeKey);
            });
        }

        var layout = value && value.layout || {};
        Object.keys(layout).forEach(function (alias) {
            var items = layout[alias];
            if (!angular.isArray(items)) {
                return;
            }

            var normalizedAlias = alias.toLowerCase();
            var includeAreas = normalizedAlias === blockGridEditorAlias.toLowerCase();
            var isList = normalizedAlias === blockListEditorAlias.toLowerCase();
            if (!isList && !includeAreas) {
                return;
            }

            if (editorAlias && normalizedAlias !== editorAlias.toLowerCase()) {
                return;
            }

            walk(items, contentTypes, counts, includeAreas);
        });

        return counts;
    }

    function walk(items, contentTypes, counts, includeAreas) {
        items.forEach(function (item) {
            var contentKey = blockKey(item && (item.contentKey || item.contentUdi));
            var contentTypeKey = contentKey ? contentTypes[contentKey] : undefined;
            if (contentTypeKey) {
                counts[contentTypeKey] = (counts[contentTypeKey] || 0) + 1;
            }

            if (!includeAreas || !item || !angular.isArray(item.areas)) {
                return;
            }

            item.areas.forEach(function (area) {
                if (area && angular.isArray(area.items) && area.items.length) {
                    walk(area.items, contentTypes, counts, includeAreas);
                }
            });
        });
    }

    function normalizeLimit(value) {
        return value === null || value === undefined ? undefined : value;
    }

    function normalizeKey(value) {
        return value ? String(value).trim().toLowerCase() : "";
    }

    function blockKey(value) {
        var text = normalizeKey(value);
        var marker = "umb://";
        if (text.indexOf(marker) !== 0) {
            return text;
        }

        var separator = text.lastIndexOf("/");
        return separator >= 0 && separator < text.length - 1 ? text.substring(separator + 1) : "";
    }
})();
