using System.Runtime.InteropServices;
using static VirtualScreen.Encoding.NvEncodeAPI;

namespace VirtualScreen.Encoding;

/// <summary>
/// NVIDIA NVENC H.265/H.264 encoder with D3D11 texture input.
/// </summary>
public unsafe class NvencEncoder : IDisposable
{
    private void* _encoder;
    private void* _d3d11Device;
    private void* _bitstreamBuffer;
    private void* _registeredResource;
    private bool _initialized;

    private NV_ENCODE_API_FUNCTION_LIST* _apiPtr;

    private readonly int _width;
    private readonly int _height;
    private readonly int _bitrate;
    private uint _frameIndex;

    private int _encodedFrameCount;

    private bool _forceNextIDR = false;

    public void ForceNextIDR() => _forceNextIDR = true;

    public NvencEncoder(IntPtr d3d11Device, int width, int height, int bitrate = 15_000_000)
    {
        _d3d11Device = (void*)d3d11Device;
        _width = width;
        _height = height;
        _bitrate = bitrate;

        Initialize();
    }

    private void Initialize()
    {
        try
        {
            _apiPtr = (NV_ENCODE_API_FUNCTION_LIST*)
                NativeMemory.AllocZeroed((nuint)sizeof(NV_ENCODE_API_FUNCTION_LIST));

            _apiPtr->version = StructVersion(2);

            var status = NvEncodeAPICreateInstance(_apiPtr);
            CheckStatus(status, "NvEncodeAPICreateInstance");

            uint maxVer = 0;
            NvEncodeAPIGetMaxSupportedVersion(&maxVer);

            var openParams = NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS.Create();
            openParams.deviceType = NV_ENC_DEVICE_TYPE.NV_ENC_DEVICE_TYPE_DIRECTX;
            openParams.device = _d3d11Device;

            var openSessionEx = Marshal.GetDelegateForFunctionPointer<NvEncOpenEncodeSessionEx>(
                (IntPtr)_apiPtr->nvEncOpenEncodeSessionEx);

            void* encoderTemp = null;
            status = openSessionEx(&openParams, &encoderTemp);
            CheckStatus(status, "NvEncOpenEncodeSessionEx");
            _encoder = encoderTemp;

            var presetConfig = NV_ENC_PRESET_CONFIG.Create();

            uint expectedPresetVersion = StructVersion(4);
            uint expectedConfigVersion = StructVersion(8);
            uint expectedRcVersion = StructVersion(1);

            var getPresetConfig = Marshal.GetDelegateForFunctionPointer<NvEncGetEncodePresetConfigEx>(
                (IntPtr)_apiPtr->nvEncGetEncodePresetConfigEx);

            GUID hevcGuid = GUID.HEVC;
            GUID p1Guid = GUID.Preset_P1;

            status = getPresetConfig(
                _encoder,
                hevcGuid,
                p1Guid,
                NV_ENC_TUNING_INFO.NV_ENC_TUNING_INFO_ULTRA_LOW_LATENCY,
                &presetConfig);

            CheckStatus(status, "NvEncGetEncodePresetConfigEx");

            var config = presetConfig.presetCfg;

            config.gopLength = 60;
            config.frameIntervalP = 1;

            // HEVC specific configuration
            config.encodeCodecConfig.hevcConfig.idrPeriod = 60;
            config.encodeCodecConfig.hevcConfig.repeatSPSPPS = 1;
            config.encodeCodecConfig.hevcConfig.outputAUD = 1;
            config.encodeCodecConfig.hevcConfig.enableLTR = 0;
            config.encodeCodecConfig.hevcConfig.ltrNumFrames = 0;
            config.encodeCodecConfig.hevcConfig.maxNumRefFramesInDPB = 1;

            // Rate control
            config.rcParams.rateControlMode = NV_ENC_PARAMS_RC_MODE.NV_ENC_PARAMS_RC_CBR;
            config.rcParams.averageBitRate = (uint)_bitrate;
            config.rcParams.maxBitRate = (uint)_bitrate;
            config.rcParams.vbvBufferSize = (uint)_bitrate / 60;
            config.rcParams.vbvInitialDelay = 0;
            //config.rcParams.enableAQ = 1;

            var initParams = NV_ENC_INITIALIZE_PARAMS.Create();
            initParams.encodeGUID = hevcGuid;
            initParams.presetGUID = p1Guid;
            initParams.encodeWidth = (uint)_width;
            initParams.encodeHeight = (uint)_height;
            initParams.darWidth = (uint)_width;
            initParams.darHeight = (uint)_height;
            initParams.frameRateNum = 60;
            initParams.frameRateDen = 1;
            initParams.enablePTD = 1;
            initParams.tuningInfo = NV_ENC_TUNING_INFO.NV_ENC_TUNING_INFO_ULTRA_LOW_LATENCY;
            initParams.bufferFormat = NV_ENC_BUFFER_FORMAT.NV_ENC_BUFFER_FORMAT_ARGB;

            var configPtr = (NV_ENC_CONFIG*)NativeMemory.AllocZeroed((nuint)sizeof(NV_ENC_CONFIG));
            *configPtr = config;
            initParams.encodeConfig = configPtr;

            var initEncoder = Marshal.GetDelegateForFunctionPointer<NvEncInitializeEncoder>(
                (IntPtr)_apiPtr->nvEncInitializeEncoder);

            try
            {
                status = initEncoder(_encoder, &initParams);
            }
            finally
            {
                NativeMemory.Free(configPtr);
            }

            CheckStatus(status, "NvEncInitializeEncoder");

            var createBitstream = NV_ENC_CREATE_BITSTREAM_BUFFER.Create();

            var createBitstreamBuffer = Marshal.GetDelegateForFunctionPointer<NvEncCreateBitstreamBuffer>(
                (IntPtr)_apiPtr->nvEncCreateBitstreamBuffer);

            status = createBitstreamBuffer(_encoder, &createBitstream);
            CheckStatus(status, "NvEncCreateBitstreamBuffer");

            _bitstreamBuffer = createBitstream.bitstreamBuffer;

            _initialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Init FAILED: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            CleanupResources();
            throw;
        }
    }

    /// <summary>
    /// Encode a D3D11 Texture2D to H.265/H.264 bitstream.
    /// CALLER must return the rented buffer: ArrayPool&lt;byte&gt;.Shared.Return(result.Buffer)
    /// </summary>
    public (byte[] Buffer, int Length, uint FrameNumber)? EncodeTexture(IntPtr d3d11Texture)
    {
        if (!_initialized) return null;

        var texturePtr = (void*)d3d11Texture;
        void* mappedResource = null;

        try
        {
            // register texture on first call
            if (_registeredResource == null)
            {
                var registerParams = NV_ENC_REGISTER_RESOURCE.Create();
                registerParams.resourceType = NV_ENC_INPUT_RESOURCE_TYPE.NV_ENC_INPUT_RESOURCE_TYPE_DIRECTX;
                registerParams.resourceToRegister = texturePtr;
                registerParams.width = (uint)_width;
                registerParams.height = (uint)_height;
                registerParams.pitch = 0;
                registerParams.bufferFormat = NV_ENC_BUFFER_FORMAT.NV_ENC_BUFFER_FORMAT_ARGB;

                var registerResource = Marshal.GetDelegateForFunctionPointer<NvEncRegisterResource>(
                    (IntPtr)_apiPtr->nvEncRegisterResource);

                var regStatus = registerResource(_encoder, &registerParams);
                CheckStatus(regStatus, "NvEncRegisterResource");
                _registeredResource = registerParams.registeredResource;
            }

            // map for this frame
            var mapParams = NV_ENC_MAP_INPUT_RESOURCE.Create();
            mapParams.registeredResource = _registeredResource;

            var mapInputResource = Marshal.GetDelegateForFunctionPointer<NvEncMapInputResource>(
                (IntPtr)_apiPtr->nvEncMapInputResource);

            var mapStatus = mapInputResource(_encoder, &mapParams);
            CheckStatus(mapStatus, "NvEncMapInputResource");
            mappedResource = mapParams.mappedResource;

            var picParams = NV_ENC_PIC_PARAMS.Create();
            picParams.inputBuffer = mappedResource;
            picParams.bufferFmt = NV_ENC_BUFFER_FORMAT.NV_ENC_BUFFER_FORMAT_ARGB;
            picParams.inputWidth = (uint)_width;
            picParams.inputHeight = (uint)_height;
            picParams.inputPitch = 0;
            picParams.outputBitstream = _bitstreamBuffer;
            picParams.completionEvent = null;
            picParams.pictureStruct = NV_ENC_PIC_STRUCT.NV_ENC_PIC_STRUCT_FRAME;

            if (_frameIndex == 0 || _forceNextIDR)
            {
                picParams.pictureType = NV_ENC_PIC_TYPE.NV_ENC_PIC_TYPE_IDR;
                _forceNextIDR = false;
            }

            picParams.frameIdx = _frameIndex;
            picParams.inputTimeStamp = (ulong)DateTime.UtcNow.Ticks;
            _frameIndex++;

            var encodePicture = Marshal.GetDelegateForFunctionPointer<NvEncEncodePicture>(
                (IntPtr)_apiPtr->nvEncEncodePicture);

            var encodeStatus = encodePicture(_encoder, &picParams);

            if (encodeStatus == NVENCSTATUS.NV_ENC_ERR_NEED_MORE_INPUT)
                return null;

            CheckStatus(encodeStatus, "NvEncEncodePicture");

            var lockParams = NV_ENC_LOCK_BITSTREAM.Create();
            lockParams.outputBitstream = _bitstreamBuffer;

            var lockBitstream = Marshal.GetDelegateForFunctionPointer<NvEncLockBitstream>(
                (IntPtr)_apiPtr->nvEncLockBitstream);

            var lockStatus = lockBitstream(_encoder, &lockParams);
            CheckStatus(lockStatus, "NvEncLockBitstream");

            var encodedSize = (int)lockParams.bitstreamSizeInBytes;

            _encodedFrameCount++;

            var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(encodedSize);
            Marshal.Copy((IntPtr)lockParams.bitstreamBufferPtr, buffer, 0, encodedSize);

            var unlockBitstream = Marshal.GetDelegateForFunctionPointer<NvEncUnlockBitstream>(
                (IntPtr)_apiPtr->nvEncUnlockBitstream);
            unlockBitstream(_encoder, _bitstreamBuffer);

            return (buffer, encodedSize, (uint)_encodedFrameCount);
        }
        finally
        {
            if (mappedResource != null)
            {
                var unmapInputResource = Marshal.GetDelegateForFunctionPointer<NvEncUnmapInputResource>(
                    (IntPtr)_apiPtr->nvEncUnmapInputResource);
                unmapInputResource(_encoder, mappedResource);
            }
        }
    }

    private static void CheckStatus(NVENCSTATUS status, string operation)
    {
        if (status == NVENCSTATUS.NV_ENC_SUCCESS) return;

        var description = status switch
        {
            NVENCSTATUS.NV_ENC_SUCCESS => "NV_ENC_SUCCESS",
            NVENCSTATUS.NV_ENC_ERR_NO_ENCODE_DEVICE => "NV_ENC_ERR_NO_ENCODE_DEVICE",
            NVENCSTATUS.NV_ENC_ERR_UNSUPPORTED_DEVICE => "NV_ENC_ERR_UNSUPPORTED_DEVICE",
            NVENCSTATUS.NV_ENC_ERR_INVALID_ENCODERDEVICE => "NV_ENC_ERR_INVALID_ENCODERDEVICE",
            NVENCSTATUS.NV_ENC_ERR_INVALID_DEVICE => "NV_ENC_ERR_INVALID_DEVICE",
            NVENCSTATUS.NV_ENC_ERR_DEVICE_NOT_EXIST => "NV_ENC_ERR_DEVICE_NOT_EXIST",
            NVENCSTATUS.NV_ENC_ERR_INVALID_PTR => "NV_ENC_ERR_INVALID_PTR",
            NVENCSTATUS.NV_ENC_ERR_INVALID_EVENT => "NV_ENC_ERR_INVALID_EVENT",
            NVENCSTATUS.NV_ENC_ERR_INVALID_PARAM => "NV_ENC_ERR_INVALID_PARAM",
            NVENCSTATUS.NV_ENC_ERR_INVALID_CALL => "NV_ENC_ERR_INVALID_CALL",
            NVENCSTATUS.NV_ENC_ERR_OUT_OF_MEMORY => "NV_ENC_ERR_OUT_OF_MEMORY",
            NVENCSTATUS.NV_ENC_ERR_ENCODER_NOT_INITIALIZED => "NV_ENC_ERR_ENCODER_NOT_INITIALIZED",
            NVENCSTATUS.NV_ENC_ERR_UNSUPPORTED_PARAM => "NV_ENC_ERR_UNSUPPORTED_PARAM",
            NVENCSTATUS.NV_ENC_ERR_LOCK_BUSY => "NV_ENC_ERR_LOCK_BUSY",
            NVENCSTATUS.NV_ENC_ERR_NOT_ENOUGH_BUFFER => "NV_ENC_ERR_NOT_ENOUGH_BUFFER",
            NVENCSTATUS.NV_ENC_ERR_INVALID_VERSION => "NV_ENC_ERR_INVALID_VERSION",
            NVENCSTATUS.NV_ENC_ERR_MAP_FAILED => "NV_ENC_ERR_MAP_FAILED",
            NVENCSTATUS.NV_ENC_ERR_NEED_MORE_INPUT => "NV_ENC_ERR_NEED_MORE_INPUT",
            NVENCSTATUS.NV_ENC_ERR_ENCODER_BUSY => "NV_ENC_ERR_ENCODER_BUSY",
            NVENCSTATUS.NV_ENC_ERR_EVENT_NOT_REGISTERD => "NV_ENC_ERR_EVENT_NOT_REGISTERD",
            NVENCSTATUS.NV_ENC_ERR_GENERIC => "NV_ENC_ERR_GENERIC",
            NVENCSTATUS.NV_ENC_ERR_INCOMPATIBLE_CLIENT_KEY => "NV_ENC_ERR_INCOMPATIBLE_CLIENT_KEY",
            NVENCSTATUS.NV_ENC_ERR_UNIMPLEMENTED => "NV_ENC_ERR_UNIMPLEMENTED",
            NVENCSTATUS.NV_ENC_ERR_RESOURCE_REGISTER_FAILED => "NV_ENC_ERR_RESOURCE_REGISTER_FAILED",
            NVENCSTATUS.NV_ENC_ERR_RESOURCE_NOT_REGISTERED => "NV_ENC_ERR_RESOURCE_NOT_REGISTERED",
            NVENCSTATUS.NV_ENC_ERR_RESOURCE_NOT_MAPPED => "NV_ENC_ERR_RESOURCE_NOT_MAPPED",
            NVENCSTATUS.NV_ENC_ERR_NEED_MORE_OUTPUT => "NV_ENC_ERR_NEED_MORE_OUTPUT",
            _ => $"Code {(int)status}"
        };

        throw new Exception($"NVENC {operation} failed: {status} — {description}");
    }

    private void CleanupResources()
    {
        try
        {
            if (_registeredResource != null && _apiPtr != null && _encoder != null)
            {
                var unregisterResource = Marshal.GetDelegateForFunctionPointer<NvEncUnregisterResource>(
                    (IntPtr)_apiPtr->nvEncUnregisterResource);
                unregisterResource(_encoder, _registeredResource);
                _registeredResource = null;
            }

            if (_bitstreamBuffer != null && _apiPtr != null && _encoder != null)
            {
                var destroyBitstream = Marshal.GetDelegateForFunctionPointer<NvEncDestroyBitstreamBuffer>(
                    (IntPtr)_apiPtr->nvEncDestroyBitstreamBuffer);
                destroyBitstream(_encoder, _bitstreamBuffer);
                _bitstreamBuffer = null;
            }

            if (_encoder != null && _apiPtr != null)
            {
                var destroyEncoder = Marshal.GetDelegateForFunctionPointer<NvEncDestroyEncoder>(
                    (IntPtr)_apiPtr->nvEncDestroyEncoder);
                destroyEncoder(_encoder);
                _encoder = null;
            }

            if (_apiPtr != null)
            {
                NativeMemory.Free(_apiPtr);
                _apiPtr = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cleanup error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_initialized) return;

        CleanupResources();
        _initialized = false;
        GC.SuppressFinalize(this);
    }
}
