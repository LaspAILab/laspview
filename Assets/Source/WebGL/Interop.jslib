mergeInto(LibraryManager.library, {
    SendResultToJS: function(errCode, result) {
        var resultStr = UTF8ToString(result);

        // 创建自定义事件
        var event = new CustomEvent('UnityMessage', {
            detail: {
                err: errCode,
                result: resultStr
            }
        });
        document.dispatchEvent(event);
    }
});