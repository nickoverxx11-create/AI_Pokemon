var WebBLEPlugin =
{
    $unityMethods: {},

    InitMethods: function(return_RequestDevice, return_Connect, callback_Disconnected, return_getCharacteristic, return_getCharacteristics, return_ReadValue, callback_StartNotifications)
    {
        if (typeof UTF8ToString === "undefined") UTF8ToString = Pointer_stringify;

        unityMethods.return_RequestDevice = return_RequestDevice;
        unityMethods.return_Connect = return_Connect;
        unityMethods.callback_Disconnected = callback_Disconnected;
        unityMethods.return_getCharacteristic = return_getCharacteristic;
        unityMethods.return_getCharacteristics = return_getCharacteristics;
        unityMethods.return_ReadValue = return_ReadValue;
        unityMethods.callback_StartNotifications = callback_StartNotifications;

        return 0;
    },

    InitErrorMethods: function(error_RequestDevice)
    {
        unityMethods.error_RequestDevice = error_RequestDevice;
        return 0;
    },

    call_RequestDevice: function(SERVICE_UUID)
    {
        var str = UTF8ToString(SERVICE_UUID);
        bluetooth_requestDevice(str, return_call_RequestDevice, error_call_RequestDevice);
        return 0;
    },

    $return_call_RequestDevice: function(id, uuid, name)
    {
        // uuid
        var size = lengthBytesUTF8(uuid) + 1;
        var ptr_uuid = _malloc(size);
        stringToUTF8(uuid, ptr_uuid, size);
        // name
        size = lengthBytesUTF8(name) + 1;
        var ptr_name = _malloc(size);
        stringToUTF8(name, ptr_name, size);

        // Runtime.dynCall('viii', ...)  ->  wasmTable.get(ptr)(...)
        Module.wasmTable.get(unityMethods.return_RequestDevice)(id, ptr_uuid, ptr_name);
    },

    $error_call_RequestDevice: function(msg)
    {
        var size = lengthBytesUTF8(msg) + 1;
        var ptr = _malloc(size);
        stringToUTF8(msg, ptr, size);

        Module.wasmTable.get(unityMethods.error_RequestDevice)(ptr);
    },

    call_Connect: function(deviceID, SERVICE_UUID)
    {
        var str = UTF8ToString(SERVICE_UUID);
        server_connect(deviceID, str, return_call_Connect, callback_Disconnected);
        return 0;
    },

    $return_call_Connect: function(deviceID, serverID, serviceID, serviceUUID)
    {
        var size = lengthBytesUTF8(serviceUUID) + 1;
        var ptr = _malloc(size);
        stringToUTF8(serviceUUID, ptr, size);

        Module.wasmTable.get(unityMethods.return_Connect)(deviceID, serverID, serviceID, ptr);
    },

    $callback_Disconnected: function(deviceID)
    {
        Module.wasmTable.get(unityMethods.callback_Disconnected)(deviceID);
    },

    call_Disconnect: function(serverID)
    {
        server_disconnect(serverID);
        return 0;
    },

    call_getCharacteristic: function(serviceID, characteristicUUID)
    {
        var str = UTF8ToString(characteristicUUID);
        service_getCharacteristic(serviceID, str, return_call_getCharacteristic);
        return 0;
    },

    $return_call_getCharacteristic: function(serviceID, id, characteristicUUID)
    {
        var size = lengthBytesUTF8(characteristicUUID) + 1;
        var ptr = _malloc(size);
        stringToUTF8(characteristicUUID, ptr, size);

        Module.wasmTable.get(unityMethods.return_getCharacteristic)(serviceID, id, ptr);
    },

    call_getCharacteristics: function(serviceID)
    {
        service_getCharacteristics(serviceID, return_call_getCharacteristics);
        return 0;
    },

    $return_call_getCharacteristics: function(serviceID, len, idx, id, uuid)
    {
        var size = lengthBytesUTF8(uuid) + 1;
        var ptr = _malloc(size);
        stringToUTF8(uuid, ptr, size);

        Module.wasmTable.get(unityMethods.return_getCharacteristics)(serviceID, len, idx, id, ptr);
    },

    $arrFromPtr: function(ptr, size, heap)
    {
        var startIndex = ptr / heap.BYTES_PER_ELEMENT;
        return heap.subarray(startIndex, startIndex + size);
    },

    call_WriteValue: function(characteristicID, pByteArr, pByteArrLen)
    {
        var byteArr = arrFromPtr(pByteArr, pByteArrLen, HEAPU8);
        characteristic_writeValue(characteristicID, byteArr);
        return 0;
    },

    call_ReadValue: function(characteristicID)
    {
        characteristic_readValue(characteristicID, return_call_ReadValue);
        return 0;
    },

    $return_call_ReadValue: function(characteristicID, resBuffer)
    {
        var ptr = _malloc(resBuffer.byteLength);
        HEAP8.set(new Uint8Array(resBuffer), ptr);

        Module.wasmTable.get(unityMethods.return_ReadValue)(characteristicID, ptr, resBuffer.byteLength);
        _free(ptr);
    },

    call_startNotifications: function(characteristicID)
    {
        characteristic_startNotifications(characteristicID, callback_call_startNotifications);
        return 0;
    },

    $callback_call_startNotifications: function(characteristicID, resBuffer)
    {
        var ptr = _malloc(resBuffer.byteLength);
        HEAP8.set(new Uint8Array(resBuffer), ptr);

        Module.wasmTable.get(unityMethods.callback_StartNotifications)(characteristicID, ptr, resBuffer.byteLength);
        _free(ptr);
    },

    call_stopNotifications: function(characteristicID)
    {
        characteristic_stopNotifications(characteristicID);
        return 0;
    }
}

autoAddDeps(WebBLEPlugin, '$unityMethods');
autoAddDeps(WebBLEPlugin, '$return_call_RequestDevice');
autoAddDeps(WebBLEPlugin, '$error_call_RequestDevice');
autoAddDeps(WebBLEPlugin, '$return_call_Connect');
autoAddDeps(WebBLEPlugin, '$callback_Disconnected');
autoAddDeps(WebBLEPlugin, '$return_call_getCharacteristic');
autoAddDeps(WebBLEPlugin, '$return_call_getCharacteristics');
autoAddDeps(WebBLEPlugin, '$arrFromPtr');
autoAddDeps(WebBLEPlugin, '$return_call_ReadValue');
autoAddDeps(WebBLEPlugin, '$callback_call_startNotifications');
mergeInto(LibraryManager.library, WebBLEPlugin);
