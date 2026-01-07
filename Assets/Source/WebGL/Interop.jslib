mergeInto(LibraryManager.library, {
    SendJSEvent: function (eventName, result) {
        var resultStr = UTF8ToString(result);
        var eventNameStr = UTF8ToString(eventName);

        // 创建自定义事件
        var event = new CustomEvent(eventNameStr, {
            detail: {
                result: resultStr
            }
        });
        document.dispatchEvent(event);
    },
});
