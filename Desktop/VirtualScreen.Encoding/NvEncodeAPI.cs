using System.Runtime.InteropServices;

namespace VirtualScreen.Encoding;

/// <summary>
/// P/Invoke declarations for NVIDIA Video Encoder API (NVENC)
/// Based on nvEncodeAPI.h v12.2
/// Struct sizes validated against C++ reference (64-bit):
///   GUID                                  =    16 bytes
///   NV_ENC_CLOCK_TIMESTAMP_SET            =     8 bytes
///   NV_ENC_TIME_CODE                      =    32 bytes
///   NV_ENC_QP                             =    12 bytes
///   NV_ENC_RC_PARAMS                      =   128 bytes
///   NV_ENC_CONFIG_H264_VUI_PARAMETERS     =   112 bytes
///   NV_ENC_CONFIG_H264                    =  1792 bytes
///   NV_ENC_CONFIG_HEVC                    =  1560 bytes
///   NV_ENC_CODEC_CONFIG                   =  1792 bytes (union)
///   NV_ENC_CONFIG                         =  3584 bytes
///   NV_ENC_PRESET_CONFIG                  =  5128 bytes
///   NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS  =  1552 bytes
///   NV_ENC_INITIALIZE_PARAMS              =  1800 bytes
///   NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE =  16 bytes
///   NV_ENC_REGISTER_RESOURCE              =  1536 bytes
///   NV_ENC_MAP_INPUT_RESOURCE             =  1544 bytes
///   NV_ENC_CREATE_INPUT_BUFFER            =   776 bytes
///   NV_ENC_LOCK_INPUT_BUFFER              =  1544 bytes
///   NV_ENC_PIC_PARAMS                     =  3360 bytes
///   NV_ENC_CODEC_PIC_PARAMS               =  1544 bytes (union)
///   NV_ENC_PIC_PARAMS_H264                =  1536 bytes
///   NV_ENC_PIC_PARAMS_HEVC                =  1536 bytes
///   NV_ENC_PIC_PARAMS_AV1                 =  1544 bytes
///   NV_ENC_PIC_PARAMS_H264_EXT            =   128 bytes (union)
///   NV_ENC_PIC_PARAMS_MVC                 =   128 bytes
///   NV_ENC_LOCK_BITSTREAM                 =  1544 bytes
///   NV_ENC_CREATE_BITSTREAM_BUFFER        =   776 bytes
///   NV_ENCODE_API_FUNCTION_LIST           =  2552 bytes
///   NV_ENC_CONFIG.rcParams                =    40
///   NV_ENC_CONFIG.encodeCodecConfig       =   168
///   NV_ENC_INITIALIZE_PARAMS.encodeConfig =    88
///   NV_ENC_PIC_PARAMS.inputBuffer         =    40
///   NV_ENC_PIC_PARAMS.outputBitstream     =    48
/// Architecture: 64-bit (sizeof(void*) = 8 bytes)
/// </summary>
internal static unsafe class NvEncodeAPI
{
    private const string NvEncDll = "nvEncodeAPI64.dll";

    // ═══════════════════════════════════════════════════════════════
    // NVENC Enums
    // ═══════════════════════════════════════════════════════════════

    public enum NV_ENC_DEVICE_TYPE : uint
    {
        NV_ENC_DEVICE_TYPE_DIRECTX = 0,
        NV_ENC_DEVICE_TYPE_CUDA = 1,
        NV_ENC_DEVICE_TYPE_OPENGL = 2,
    }

    public enum NV_ENC_BUFFER_FORMAT : uint
    {
        NV_ENC_BUFFER_FORMAT_UNDEFINED = 0,
        NV_ENC_BUFFER_FORMAT_NV12 = 0x00000001,
        NV_ENC_BUFFER_FORMAT_YV12 = 0x00000010,
        NV_ENC_BUFFER_FORMAT_IYUV = 0x00000100,
        NV_ENC_BUFFER_FORMAT_YUV444 = 0x00001000,
        NV_ENC_BUFFER_FORMAT_YUV420_10BIT = 0x00010000,
        NV_ENC_BUFFER_FORMAT_YUV444_10BIT = 0x00100000,
        NV_ENC_BUFFER_FORMAT_ARGB = 0x01000000,
        NV_ENC_BUFFER_FORMAT_ARGB10 = 0x02000000,
        NV_ENC_BUFFER_FORMAT_AYUV = 0x04000000,
        NV_ENC_BUFFER_FORMAT_ABGR = 0x10000000,
        NV_ENC_BUFFER_FORMAT_ABGR10 = 0x20000000,
        NV_ENC_BUFFER_FORMAT_U8 = 0x40000000,
    }

    public enum NV_ENC_PIC_TYPE : uint
    {
        NV_ENC_PIC_TYPE_P = 0x0,
        NV_ENC_PIC_TYPE_B = 0x01,
        NV_ENC_PIC_TYPE_I = 0x02,
        NV_ENC_PIC_TYPE_IDR = 0x03,
        NV_ENC_PIC_TYPE_BI = 0x04,
        NV_ENC_PIC_TYPE_SKIPPED = 0x05,
        NV_ENC_PIC_TYPE_INTRA_REFRESH = 0x06,
        NV_ENC_PIC_TYPE_NONREF_P = 0x07,
        NV_ENC_PIC_TYPE_SWITCH = 0x08,
        NV_ENC_PIC_TYPE_UNKNOWN = 0xFF
    }

    public enum NV_ENC_TUNING_INFO : uint
    {
        NV_ENC_TUNING_INFO_UNDEFINED = 0,
        NV_ENC_TUNING_INFO_HIGH_QUALITY = 1,
        NV_ENC_TUNING_INFO_LOW_LATENCY = 2,
        NV_ENC_TUNING_INFO_ULTRA_LOW_LATENCY = 3,
        NV_ENC_TUNING_INFO_LOSSLESS = 4,
        NV_ENC_TUNING_INFO_ULTRA_HIGH_QUALITY = 5,
        NV_ENC_TUNING_INFO_COUNT
    }

    public enum NV_ENC_SPLIT_ENCODE_MODE : uint
    {
        NV_ENC_SPLIT_AUTO_MODE = 0,
        NV_ENC_SPLIT_AUTO_FORCED_MODE = 1,
        NV_ENC_SPLIT_TWO_FORCED_MODE = 2,
        NV_ENC_SPLIT_THREE_FORCED_MODE = 3,
        NV_ENC_SPLIT_DISABLE_MODE = 15,
    }

    public enum NV_ENC_PARAMS_RC_MODE : uint
    {
        NV_ENC_PARAMS_RC_CONSTQP = 0,
        NV_ENC_PARAMS_RC_VBR = 1,
        NV_ENC_PARAMS_RC_CBR = 2,
        NV_ENC_PARAMS_RC_CBR_LOWDELAY_HQ = 8,
        NV_ENC_PARAMS_RC_CBR_HQ = 16,
        NV_ENC_PARAMS_RC_VBR_HQ = 32
    }

    public enum NV_ENC_MULTI_PASS : uint
    {
        NV_ENC_MULTI_PASS_DISABLED = 0,
        NV_ENC_TWO_PASS_QUARTER_RESOLUTION = 1,
        NV_ENC_TWO_PASS_FULL_RESOLUTION = 2
    }

    public enum NV_ENC_EMPHASIS_MAP_LEVEL : uint
    {
        NV_ENC_EMPHASIS_MAP_LEVEL_0 = 0x0,
        NV_ENC_EMPHASIS_MAP_LEVEL_1 = 0x1,
        NV_ENC_EMPHASIS_MAP_LEVEL_2 = 0x2,
        NV_ENC_EMPHASIS_MAP_LEVEL_3 = 0x3,
        NV_ENC_EMPHASIS_MAP_LEVEL_4 = 0x4,
        NV_ENC_EMPHASIS_MAP_LEVEL_5 = 0x5
    }

    public enum NV_ENC_QP_MAP_MODE : uint
    {
        NV_ENC_QP_MAP_DISABLED = 0,
        NV_ENC_QP_MAP_EMPHASIS = 1,
        NV_ENC_QP_MAP_DELTA = 2,
        NV_ENC_QP_MAP = 3
    }

    public enum NV_ENC_HEVC_CUSIZE : uint
    {
        NV_ENC_HEVC_CUSIZE_AUTOSELECT = 0,
        NV_ENC_HEVC_CUSIZE_8x8 = 1,
        NV_ENC_HEVC_CUSIZE_16x16 = 2,
        NV_ENC_HEVC_CUSIZE_32x32 = 3,
        NV_ENC_HEVC_CUSIZE_64x64 = 4,
    }

    public enum NV_ENC_BFRAME_REF_MODE : uint
    {
        NV_ENC_BFRAME_REF_MODE_DISABLED = 0x0,
        NV_ENC_BFRAME_REF_MODE_EACH = 0x1,
        NV_ENC_BFRAME_REF_MODE_MIDDLE = 0x2,
    }

    public enum NV_ENC_NUM_REF_FRAMES : uint
    {
        NV_ENC_NUM_REF_FRAMES_AUTOSELECT = 0,
        NV_ENC_NUM_REF_FRAMES_1 = 1,
        NV_ENC_NUM_REF_FRAMES_2 = 2,
        NV_ENC_NUM_REF_FRAMES_3 = 3,
        NV_ENC_NUM_REF_FRAMES_4 = 4,
        NV_ENC_NUM_REF_FRAMES_5 = 5,
        NV_ENC_NUM_REF_FRAMES_6 = 6,
        NV_ENC_NUM_REF_FRAMES_7 = 7,
    }

    public enum NV_ENC_H264_ENTROPY_CODING_MODE : uint
    {
        NV_ENC_H264_ENTROPY_CODING_MODE_AUTOSELECT = 0x0,
        NV_ENC_H264_ENTROPY_CODING_MODE_CABAC = 0x1,
        NV_ENC_H264_ENTROPY_CODING_MODE_CAVLC = 0x2
    }

    public enum NV_ENC_H264_BDIRECT_MODE : uint
    {
        NV_ENC_H264_BDIRECT_MODE_AUTOSELECT = 0x0,
        NV_ENC_H264_BDIRECT_MODE_DISABLE = 0x1,
        NV_ENC_H264_BDIRECT_MODE_TEMPORAL = 0x2,
        NV_ENC_H264_BDIRECT_MODE_SPATIAL = 0x3
    }

    public enum NV_ENC_H264_FMO_MODE : uint
    {
        NV_ENC_H264_FMO_AUTOSELECT = 0x0,
        NV_ENC_H264_FMO_ENABLE = 0x1,
        NV_ENC_H264_FMO_DISABLE = 0x2,
    }

    public enum NV_ENC_H264_ADAPTIVE_TRANSFORM_MODE : uint
    {
        NV_ENC_H264_ADAPTIVE_TRANSFORM_AUTOSELECT = 0x0,
        NV_ENC_H264_ADAPTIVE_TRANSFORM_DISABLE = 0x1,
        NV_ENC_H264_ADAPTIVE_TRANSFORM_ENABLE = 0x2,
    }

    public enum NV_ENC_STEREO_PACKING_MODE : uint
    {
        NV_ENC_STEREO_PACKING_MODE_NONE = 0x0,
        NV_ENC_STEREO_PACKING_MODE_CHECKERBOARD = 0x1,
        NV_ENC_STEREO_PACKING_MODE_COLINTERLEAVE = 0x2,
        NV_ENC_STEREO_PACKING_MODE_ROWINTERLEAVE = 0x3,
        NV_ENC_STEREO_PACKING_MODE_SIDEBYSIDE = 0x4,
        NV_ENC_STEREO_PACKING_MODE_TOPBOTTOM = 0x5,
        NV_ENC_STEREO_PACKING_MODE_FRAMESEQ = 0x6
    }

    public enum NV_ENC_TEMPORAL_FILTER_LEVEL : uint
    {
        NV_ENC_TEMPORAL_FILTER_LEVEL_0 = 0,
        NV_ENC_TEMPORAL_FILTER_LEVEL_4 = 4,
    }

    public enum NV_ENC_CAPS : uint
    {
        NV_ENC_CAPS_NUM_MAX_BFRAMES,
        NV_ENC_CAPS_SUPPORTED_RATECONTROL_MODES,
        NV_ENC_CAPS_SUPPORT_FIELD_ENCODING,
        NV_ENC_CAPS_SUPPORT_MONOCHROME,
        NV_ENC_CAPS_SUPPORT_FMO,
        NV_ENC_CAPS_SUPPORT_QPELMV,
        NV_ENC_CAPS_SUPPORT_BDIRECT_MODE,
        NV_ENC_CAPS_SUPPORT_CABAC,
        NV_ENC_CAPS_SUPPORT_ADAPTIVE_TRANSFORM,
        NV_ENC_CAPS_SUPPORT_STEREO_MVC,
        NV_ENC_CAPS_NUM_MAX_TEMPORAL_LAYERS,
        NV_ENC_CAPS_SUPPORT_HIERARCHICAL_PFRAMES,
        NV_ENC_CAPS_SUPPORT_HIERARCHICAL_BFRAMES,
        NV_ENC_CAPS_LEVEL_MAX,
        NV_ENC_CAPS_LEVEL_MIN,
        NV_ENC_CAPS_SEPARATE_COLOUR_PLANE,
        NV_ENC_CAPS_WIDTH_MAX,
        NV_ENC_CAPS_HEIGHT_MAX,
        NV_ENC_CAPS_SUPPORT_TEMPORAL_SVC,
        NV_ENC_CAPS_SUPPORT_DYN_RES_CHANGE,
        NV_ENC_CAPS_SUPPORT_DYN_BITRATE_CHANGE,
        NV_ENC_CAPS_SUPPORT_DYN_FORCE_CONSTQP,
        NV_ENC_CAPS_SUPPORT_DYN_RCMODE_CHANGE,
        NV_ENC_CAPS_SUPPORT_SUBFRAME_READBACK,
        NV_ENC_CAPS_SUPPORT_CONSTRAINED_ENCODING,
        NV_ENC_CAPS_SUPPORT_INTRA_REFRESH,
        NV_ENC_CAPS_SUPPORT_CUSTOM_VBV_BUF_SIZE,
        NV_ENC_CAPS_SUPPORT_DYNAMIC_SLICE_MODE,
        NV_ENC_CAPS_SUPPORT_REF_PIC_INVALIDATION,
        NV_ENC_CAPS_PREPROC_SUPPORT,
        NV_ENC_CAPS_ASYNC_ENCODE_SUPPORT,
        NV_ENC_CAPS_MB_NUM_MAX,
        NV_ENC_CAPS_MB_PER_SEC_MAX,
        NV_ENC_CAPS_SUPPORT_YUV444_ENCODE,
        NV_ENC_CAPS_SUPPORT_LOSSLESS_ENCODE,
        NV_ENC_CAPS_SUPPORT_SAO,
        NV_ENC_CAPS_SUPPORT_MEONLY_MODE,
        NV_ENC_CAPS_SUPPORT_LOOKAHEAD,
        NV_ENC_CAPS_SUPPORT_TEMPORAL_AQ,
        NV_ENC_CAPS_SUPPORT_10BIT_ENCODE,
        NV_ENC_CAPS_NUM_MAX_LTR_FRAMES,
        NV_ENC_CAPS_SUPPORT_WEIGHTED_PREDICTION,
        NV_ENC_CAPS_DYNAMIC_QUERY_ENCODER_CAPACITY,
        NV_ENC_CAPS_SUPPORT_BFRAME_REF_MODE,
        NV_ENC_CAPS_SUPPORT_EMPHASIS_LEVEL_MAP,
        NV_ENC_CAPS_WIDTH_MIN,
        NV_ENC_CAPS_HEIGHT_MIN,
        NV_ENC_CAPS_SUPPORT_MULTIPLE_REF_FRAMES,
        NV_ENC_CAPS_SUPPORT_ALPHA_LAYER_ENCODING,
        NV_ENC_CAPS_NUM_ENCODER_ENGINES,
        NV_ENC_CAPS_SINGLE_SLICE_INTRA_REFRESH,
        NV_ENC_CAPS_DISABLE_ENC_STATE_ADVANCE,
        NV_ENC_CAPS_OUTPUT_RECON_SURFACE,
        NV_ENC_CAPS_OUTPUT_BLOCK_STATS,
        NV_ENC_CAPS_OUTPUT_ROW_STATS,
        NV_ENC_CAPS_SUPPORT_TEMPORAL_FILTER,
        NV_ENC_CAPS_SUPPORT_LOOKAHEAD_LEVEL,
        NV_ENC_CAPS_SUPPORT_UNIDIRECTIONAL_B,
        NV_ENC_CAPS_EXPOSED_COUNT
    }

    public enum NV_ENC_LOOKAHEAD_LEVEL : uint
    {
        NV_ENC_LOOKAHEAD_LEVEL_0 = 0,
        NV_ENC_LOOKAHEAD_LEVEL_1 = 1,
        NV_ENC_LOOKAHEAD_LEVEL_2 = 2,
        NV_ENC_LOOKAHEAD_LEVEL_3 = 3,
        NV_ENC_LOOKAHEAD_LEVEL_AUTOSELECT = 15,
    }

    public enum NV_ENC_BIT_DEPTH : uint
    {
        NV_ENC_BIT_DEPTH_INVALID = 0,
        NV_ENC_BIT_DEPTH_8 = 8,
        NV_ENC_BIT_DEPTH_10 = 10,
    }

    public enum NV_ENC_AV1_PART_SIZE : uint
    {
        NV_ENC_AV1_PART_SIZE_AUTOSELECT = 0,
        NV_ENC_AV1_PART_SIZE_4x4 = 1,
        NV_ENC_AV1_PART_SIZE_8x8 = 2,
        NV_ENC_AV1_PART_SIZE_16x16 = 3,
        NV_ENC_AV1_PART_SIZE_32x32 = 4,
        NV_ENC_AV1_PART_SIZE_64x64 = 5,
    }

    public enum NV_ENC_VUI_VIDEO_FORMAT : uint
    {
        NV_ENC_VUI_VIDEO_FORMAT_COMPONENT = 0,
        NV_ENC_VUI_VIDEO_FORMAT_PAL = 1,
        NV_ENC_VUI_VIDEO_FORMAT_NTSC = 2,
        NV_ENC_VUI_VIDEO_FORMAT_SECAM = 3,
        NV_ENC_VUI_VIDEO_FORMAT_MAC = 4,
        NV_ENC_VUI_VIDEO_FORMAT_UNSPECIFIED = 5,
    }

    public enum NV_ENC_VUI_COLOR_PRIMARIES : uint
    {
        NV_ENC_VUI_COLOR_PRIMARIES_UNDEFINED = 0,
        NV_ENC_VUI_COLOR_PRIMARIES_BT709 = 1,
        NV_ENC_VUI_COLOR_PRIMARIES_UNSPECIFIED = 2,
        NV_ENC_VUI_COLOR_PRIMARIES_RESERVED = 3,
        NV_ENC_VUI_COLOR_PRIMARIES_BT470M = 4,
        NV_ENC_VUI_COLOR_PRIMARIES_BT470BG = 5,
        NV_ENC_VUI_COLOR_PRIMARIES_SMPTE170M = 6,
        NV_ENC_VUI_COLOR_PRIMARIES_SMPTE240M = 7,
        NV_ENC_VUI_COLOR_PRIMARIES_FILM = 8,
        NV_ENC_VUI_COLOR_PRIMARIES_BT2020 = 9,
        NV_ENC_VUI_COLOR_PRIMARIES_SMPTE428 = 10,
        NV_ENC_VUI_COLOR_PRIMARIES_SMPTE431 = 11,
        NV_ENC_VUI_COLOR_PRIMARIES_SMPTE432 = 12,
        NV_ENC_VUI_COLOR_PRIMARIES_JEDEC_P22 = 22,

    }

    public enum NV_ENC_VUI_TRANSFER_CHARACTERISTIC : uint
    {
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_UNDEFINED = 0,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_BT709 = 1,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_UNSPECIFIED = 2,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_RESERVED = 3,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_BT470M = 4,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_BT470BG = 5,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_SMPTE170M = 6,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_SMPTE240M = 7,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_LINEAR = 8,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_LOG = 9,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_LOG_SQRT = 10,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_IEC61966_2_4 = 11,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_BT1361_ECG = 12,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_SRGB = 13,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_BT2020_10 = 14,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_BT2020_12 = 15,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_SMPTE2084 = 16,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_SMPTE428 = 17,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_ARIB_STD_B67 = 18,
    }

    public enum NV_ENC_VUI_MATRIX_COEFFS : uint
    {
        NV_ENC_VUI_MATRIX_COEFFS_RGB = 0,
        NV_ENC_VUI_MATRIX_COEFFS_BT709 = 1,
        NV_ENC_VUI_MATRIX_COEFFS_UNSPECIFIED = 2,
        NV_ENC_VUI_MATRIX_COEFFS_RESERVED = 3,
        NV_ENC_VUI_MATRIX_COEFFS_FCC = 4,
        NV_ENC_VUI_MATRIX_COEFFS_BT470BG = 5,
        NV_ENC_VUI_MATRIX_COEFFS_SMPTE170M = 6,
        NV_ENC_VUI_MATRIX_COEFFS_SMPTE240M = 7,
        NV_ENC_VUI_MATRIX_COEFFS_YCGCO = 8,
        NV_ENC_VUI_MATRIX_COEFFS_BT2020_NCL = 9,
        NV_ENC_VUI_MATRIX_COEFFS_BT2020_CL = 10,
        NV_ENC_VUI_MATRIX_COEFFS_SMPTE2085 = 11,
    }

    public enum NV_ENC_PARAMS_FRAME_FIELD_MODE : uint
    {
        NV_ENC_PARAMS_FRAME_FIELD_MODE_FRAME = 0x01,
        NV_ENC_PARAMS_FRAME_FIELD_MODE_FIELD = 0x02,
        NV_ENC_PARAMS_FRAME_FIELD_MODE_MBAFF = 0x03,
    }

    public enum NV_ENC_MV_PRECISION : uint
    {
        NV_ENC_MV_PRECISION_DEFAULT = 0x0,
        NV_ENC_MV_PRECISION_FULL_PEL = 0x01,
        NV_ENC_MV_PRECISION_HALF_PEL = 0x02,
        NV_ENC_MV_PRECISION_QUARTER_PEL = 0x03,
    }

    public enum NV_ENC_STATE_RESTORE_TYPE : uint
    {
        NV_ENC_STATE_RESTORE_FULL = 0x01,
        NV_ENC_STATE_RESTORE_RATE_CONTROL = 0x02,
        NV_ENC_STATE_RESTORE_ENCODE = 0x03,
    }

    public enum NV_ENC_OUTPUT_STATS_LEVEL : uint
    {
        NV_ENC_OUTPUT_STATS_NONE = 0,
        NV_ENC_OUTPUT_STATS_BLOCK_LEVEL = 1,
        NV_ENC_OUTPUT_STATS_ROW_LEVEL = 2,
    }

    public enum NV_ENC_INPUT_RESOURCE_TYPE : uint
    {
        NV_ENC_INPUT_RESOURCE_TYPE_DIRECTX = 0x0,
        NV_ENC_INPUT_RESOURCE_TYPE_CUDADEVICEPTR = 0x1,
        NV_ENC_INPUT_RESOURCE_TYPE_CUDAARRAY = 0x2,
        NV_ENC_INPUT_RESOURCE_TYPE_OPENGL_TEX = 0x3,
    }

    public enum NV_ENC_BUFFER_USAGE : uint
    {
        NV_ENC_INPUT_IMAGE = 0x0,
        NV_ENC_OUTPUT_MOTION_VECTOR = 0x1,
        NV_ENC_OUTPUT_BITSTREAM = 0x2,
        NV_ENC_OUTPUT_RECON = 0x4,
    }

    public enum NV_ENC_PIC_STRUCT : uint
    {
        NV_ENC_PIC_STRUCT_FRAME = 0x01,
        NV_ENC_PIC_STRUCT_FIELD_TOP_BOTTOM = 0x02,
        NV_ENC_PIC_STRUCT_FIELD_BOTTOM_TOP = 0x03
    }

    public enum NV_ENC_DISPLAY_PIC_STRUCT : uint
    {
        NV_ENC_PIC_STRUCT_DISPLAY_FRAME = 0x00,
        NV_ENC_PIC_STRUCT_DISPLAY_FIELD_TOP_BOTTOM = 0x01,
        NV_ENC_PIC_STRUCT_DISPLAY_FIELD_BOTTOM_TOP = 0x02,
        NV_ENC_PIC_STRUCT_DISPLAY_FRAME_DOUBLING = 0x03,
        NV_ENC_PIC_STRUCT_DISPLAY_FRAME_TRIPLING = 0x04
    }

    public enum NV_ENC_LEVEL : uint
    {
        NV_ENC_LEVEL_AUTOSELECT = 0,
        NV_ENC_LEVEL_H264_1 = 10,
        NV_ENC_LEVEL_H264_1b = 9,
        NV_ENC_LEVEL_H264_11 = 11,
        NV_ENC_LEVEL_H264_12 = 12,
        NV_ENC_LEVEL_H264_13 = 13,
        NV_ENC_LEVEL_H264_2 = 20,
        NV_ENC_LEVEL_H264_21 = 21,
        NV_ENC_LEVEL_H264_22 = 22,
        NV_ENC_LEVEL_H264_3 = 30,
        NV_ENC_LEVEL_H264_31 = 31,
        NV_ENC_LEVEL_H264_32 = 32,
        NV_ENC_LEVEL_H264_4 = 40,
        NV_ENC_LEVEL_H264_41 = 41,
        NV_ENC_LEVEL_H264_42 = 42,
        NV_ENC_LEVEL_H264_5 = 50,
        NV_ENC_LEVEL_H264_51 = 51,
        NV_ENC_LEVEL_H264_52 = 52,
        NV_ENC_LEVEL_H264_60 = 60,
        NV_ENC_LEVEL_H264_61 = 61,
        NV_ENC_LEVEL_H264_62 = 62,
        NV_ENC_LEVEL_HEVC_1 = 30,
        NV_ENC_LEVEL_HEVC_2 = 60,
        NV_ENC_LEVEL_HEVC_21 = 63,
        NV_ENC_LEVEL_HEVC_3 = 90,
        NV_ENC_LEVEL_HEVC_31 = 93,
        NV_ENC_LEVEL_HEVC_4 = 120,
        NV_ENC_LEVEL_HEVC_41 = 123,
        NV_ENC_LEVEL_HEVC_5 = 150,
        NV_ENC_LEVEL_HEVC_51 = 153,
        NV_ENC_LEVEL_HEVC_52 = 156,
        NV_ENC_LEVEL_HEVC_6 = 180,
        NV_ENC_LEVEL_HEVC_61 = 183,
        NV_ENC_LEVEL_HEVC_62 = 186,
        NV_ENC_TIER_HEVC_MAIN = 0,
        NV_ENC_TIER_HEVC_HIGH = 1,
        NV_ENC_LEVEL_AV1_2 = 0,
        NV_ENC_LEVEL_AV1_21 = 1,
        NV_ENC_LEVEL_AV1_22 = 2,
        NV_ENC_LEVEL_AV1_23 = 3,
        NV_ENC_LEVEL_AV1_3 = 4,
        NV_ENC_LEVEL_AV1_31 = 5,
        NV_ENC_LEVEL_AV1_32 = 6,
        NV_ENC_LEVEL_AV1_33 = 7,
        NV_ENC_LEVEL_AV1_4 = 8,
        NV_ENC_LEVEL_AV1_41 = 9,
        NV_ENC_LEVEL_AV1_42 = 10,
        NV_ENC_LEVEL_AV1_43 = 11,
        NV_ENC_LEVEL_AV1_5 = 12,
        NV_ENC_LEVEL_AV1_51 = 13,
        NV_ENC_LEVEL_AV1_52 = 14,
        NV_ENC_LEVEL_AV1_53 = 15,
        NV_ENC_LEVEL_AV1_6 = 16,
        NV_ENC_LEVEL_AV1_61 = 17,
        NV_ENC_LEVEL_AV1_62 = 18,
        NV_ENC_LEVEL_AV1_63 = 19,
        NV_ENC_LEVEL_AV1_7 = 20,
        NV_ENC_LEVEL_AV1_71 = 21,
        NV_ENC_LEVEL_AV1_72 = 22,
        NV_ENC_LEVEL_AV1_73 = 23,
        NV_ENC_LEVEL_AV1_AUTOSELECT,
        NV_ENC_TIER_AV1_0 = 0,
        NV_ENC_TIER_AV1_1 = 1
    }

    public enum NVENCSTATUS : uint
    {
        NV_ENC_SUCCESS,
        NV_ENC_ERR_NO_ENCODE_DEVICE,
        NV_ENC_ERR_UNSUPPORTED_DEVICE,
        NV_ENC_ERR_INVALID_ENCODERDEVICE,
        NV_ENC_ERR_INVALID_DEVICE,
        NV_ENC_ERR_DEVICE_NOT_EXIST,
        NV_ENC_ERR_INVALID_PTR,
        NV_ENC_ERR_INVALID_EVENT,
        NV_ENC_ERR_INVALID_PARAM,
        NV_ENC_ERR_INVALID_CALL,
        NV_ENC_ERR_OUT_OF_MEMORY,
        NV_ENC_ERR_ENCODER_NOT_INITIALIZED,
        NV_ENC_ERR_UNSUPPORTED_PARAM,
        NV_ENC_ERR_LOCK_BUSY,
        NV_ENC_ERR_NOT_ENOUGH_BUFFER,
        NV_ENC_ERR_INVALID_VERSION,
        NV_ENC_ERR_MAP_FAILED,
        NV_ENC_ERR_NEED_MORE_INPUT,
        NV_ENC_ERR_ENCODER_BUSY,
        NV_ENC_ERR_EVENT_NOT_REGISTERD,
        NV_ENC_ERR_GENERIC,
        NV_ENC_ERR_INCOMPATIBLE_CLIENT_KEY,
        NV_ENC_ERR_UNIMPLEMENTED,
        NV_ENC_ERR_RESOURCE_REGISTER_FAILED,
        NV_ENC_ERR_RESOURCE_NOT_REGISTERED,
        NV_ENC_ERR_RESOURCE_NOT_MAPPED,
        NV_ENC_ERR_NEED_MORE_OUTPUT,
    }

    public enum NV_ENC_PIC_FLAGS : uint
    {
        NV_ENC_PIC_FLAG_FORCEINTRA = 0x1,
        NV_ENC_PIC_FLAG_FORCEIDR = 0x2,
        NV_ENC_PIC_FLAG_OUTPUT_SPSPPS = 0x4,
        NV_ENC_PIC_FLAG_EOS = 0x8,
        NV_ENC_PIC_FLAG_DISABLE_ENC_STATE_ADVANCE = 0x10,
        NV_ENC_PIC_FLAG_OUTPUT_RECON_FRAME = 0x20,
    }

    public enum NV_ENC_MEMORY_HEAP : uint
    {
        NV_ENC_MEMORY_HEAP_AUTOSELECT = 0,
        NV_ENC_MEMORY_HEAP_VID = 1,
        NV_ENC_MEMORY_HEAP_SYSMEM_CACHED = 2,
        NV_ENC_MEMORY_HEAP_SYSMEM_UNCACHED = 3
    }

    // ═══════════════════════════════════════════════════════════════
    // GUID
    // ═══════════════════════════════════════════════════════════════

    [StructLayout(LayoutKind.Sequential)]
    public struct GUID
    {
        public uint Data1;
        public ushort Data2;
        public ushort Data3;
        public unsafe fixed byte Data4[8];

        // =========================================================================================
        // Encode Codec GUIDS supported by the NvEncodeAPI interface.
        // =========================================================================================

        // {6BC82762-4E63-4ca4-AA85-1E50F321F6BF}
        public static GUID H264
        {
            get
            {
                var g = new GUID { Data1 = 0x6BC82762, Data2 = 0x4E63, Data3 = 0x4CA4 };
                g.Data4[0] = 0xAA; g.Data4[1] = 0x85; g.Data4[2] = 0x1E; g.Data4[3] = 0x50;
                g.Data4[4] = 0xF3; g.Data4[5] = 0x21; g.Data4[6] = 0xF6; g.Data4[7] = 0xBF;
                return g;
            }
        }
        // {790CDC88-4522-4D7B-9530-BD841F8382C8}
        public static GUID HEVC
        {
            get
            {
                var g = new GUID { Data1 = 0x790CDC88, Data2 = 0x4522, Data3 = 0x4D7B };
                g.Data4[0] = 0x94; g.Data4[1] = 0x25; g.Data4[2] = 0xBD; g.Data4[3] = 0xA9;
                g.Data4[4] = 0x97; g.Data4[5] = 0x5F; g.Data4[6] = 0x76; g.Data4[7] = 0x03;
                return g;
            }
        }

        // {0A352289-0AA7-4759-862D-5D15CD16D254}
        public static GUID AV1
        {
            get
            {
                var g = new GUID { Data1 = 0x0A352289, Data2 = 0x0AA7, Data3 = 0x4759 };
                g.Data4[0] = 0x86; g.Data4[1] = 0x2D; g.Data4[2] = 0x5D; g.Data4[3] = 0x15;
                g.Data4[4] = 0xCD; g.Data4[5] = 0x16; g.Data4[6] = 0xD2; g.Data4[7] = 0x54;
                return g;
            }
        }

        // =========================================================================================
        // *   Encode Profile GUIDS supported by the NvEncodeAPI interface.
        // =========================================================================================

        // {BFD6F8E7-233C-4341-8B3E-4818523803F4}
        public static GUID Profile_AutoSelect
        {
            get
            {
                var g = new GUID { Data1 = 0xBFD6F8E7, Data2 = 0x233C, Data3 = 0x4341 };
                g.Data4[0] = 0x8B; g.Data4[1] = 0x3E; g.Data4[2] = 0x48; g.Data4[3] = 0x18;
                g.Data4[4] = 0x52; g.Data4[5] = 0x38; g.Data4[6] = 0x03; g.Data4[7] = 0xF4;
                return g;
            }
        }

        // {0727BCAA-78C4-4c83-8C2F-EF3DFF267C6A}
        public static GUID H264_Profile_Baseline
        {
            get
            {
                var g = new GUID { Data1 = 0x0727BCAA, Data2 = 0x78C4, Data3 = 0x4C83 };
                g.Data4[0] = 0x8C; g.Data4[1] = 0x2F; g.Data4[2] = 0xEF; g.Data4[3] = 0x3D;
                g.Data4[4] = 0xFF; g.Data4[5] = 0x26; g.Data4[6] = 0x7C; g.Data4[7] = 0x6A;
                return g;
            }
        }

        // {60B5C1D4-67FE-4790-94D5-C4726D7B6E6D}
        public static GUID H264_Profile_Main
        {
            get
            {
                var g = new GUID { Data1 = 0x60B5C1D4, Data2 = 0x67FE, Data3 = 0x4790 };
                g.Data4[0] = 0x94; g.Data4[1] = 0xD5; g.Data4[2] = 0xC4; g.Data4[3] = 0x72;
                g.Data4[4] = 0x6D; g.Data4[5] = 0x7B; g.Data4[6] = 0x6E; g.Data4[7] = 0x6D;
                return g;
            }
        }

        // {E7CBC309-4F7A-4b89-AF2A-D537C92BE310}
        public static GUID H264_Profile_High
        {
            get
            {
                var g = new GUID { Data1 = 0xE7CBC309, Data2 = 0x4F7A, Data3 = 0x4B89 };
                g.Data4[0] = 0xAF; g.Data4[1] = 0x2A; g.Data4[2] = 0xD5; g.Data4[3] = 0x37;
                g.Data4[4] = 0xC9; g.Data4[5] = 0x2B; g.Data4[6] = 0xE3; g.Data4[7] = 0x10;
                return g;
            }
        }

        // {7AC663CB-A598-4960-B844-339B261A7D52}
        public static GUID H264_Profile_High444
        {
            get
            {
                var g = new GUID { Data1 = 0x7AC663CB, Data2 = 0xA598, Data3 = 0x4960 };
                g.Data4[0] = 0xB8; g.Data4[1] = 0x44; g.Data4[2] = 0x33; g.Data4[3] = 0x9B;
                g.Data4[4] = 0x26; g.Data4[5] = 0x1A; g.Data4[6] = 0x7D; g.Data4[7] = 0x52;
                return g;
            }
        }

        // {40847BF5-33F7-4601-9084-E8FE3C1DB8B7}
        public static GUID H264_Profile_Stereo
        {
            get
            {
                var g = new GUID { Data1 = 0x40847BF5, Data2 = 0x33F7, Data3 = 0x4601 };
                g.Data4[0] = 0x90; g.Data4[1] = 0x84; g.Data4[2] = 0xE8; g.Data4[3] = 0xFE;
                g.Data4[4] = 0x3C; g.Data4[5] = 0x1D; g.Data4[6] = 0xB8; g.Data4[7] = 0xB7;
                return g;
            }
        }

        // {B405AFAC-F32B-417B-89C4-9ABEED3E5978}
        public static GUID H264_Profile_ProgressiveHigh
        {
            get
            {
                var g = new GUID { Data1 = 0xB405AFAC, Data2 = 0xF32B, Data3 = 0x417B };
                g.Data4[0] = 0x89; g.Data4[1] = 0xC4; g.Data4[2] = 0x9A; g.Data4[3] = 0xBE;
                g.Data4[4] = 0xED; g.Data4[5] = 0x3E; g.Data4[6] = 0x59; g.Data4[7] = 0x78;
                return g;
            }
        }

        // {AEC1BD87-E85B-48f2-84C3-98BCA6285072}
        public static GUID H264_Profile_ConstrainedHigh
        {
            get
            {
                var g = new GUID { Data1 = 0xAEC1BD87, Data2 = 0xE85B, Data3 = 0x48f2 };
                g.Data4[0] = 0x84; g.Data4[1] = 0xC3; g.Data4[2] = 0x98; g.Data4[3] = 0xBC;
                g.Data4[4] = 0xA6; g.Data4[5] = 0x28; g.Data4[6] = 0x50; g.Data4[7] = 0x72;
                return g;
            }
        }

        // {B514C39A-B55B-40fa-878F-F1253B4DFDEC}
        public static GUID HEVC_Profile_Main
        {
            get
            {
                var g = new GUID { Data1 = 0xB514C39A, Data2 = 0xB55B, Data3 = 0x40FA };
                g.Data4[0] = 0x87; g.Data4[1] = 0x8F; g.Data4[2] = 0xF1; g.Data4[3] = 0x25;
                g.Data4[4] = 0x3B; g.Data4[5] = 0x4D; g.Data4[6] = 0xFD; g.Data4[7] = 0xEC;
                return g;
            }
        }

        // {fa4d2b6c-3a5b-411a-8018-0a3f5e3c9be5}
        public static GUID HEVC_Profile_Main10
        {
            get
            {
                var g = new GUID { Data1 = 0xFA4D2B6C, Data2 = 0x3A5B, Data3 = 0x411A };
                g.Data4[0] = 0x80; g.Data4[1] = 0x18; g.Data4[2] = 0x0A; g.Data4[3] = 0x3F;
                g.Data4[4] = 0x5E; g.Data4[5] = 0x3C; g.Data4[6] = 0x9B; g.Data4[7] = 0xE5;
                return g;
            }
        }

        // For HEVC Main 444 8 bit and HEVC Main 444 10 bit profiles only
        // {51ec32b5-1b4c-453c-9cbd-b616bd621341}
        public static GUID HEVC_Profile_Frext
        {
            get
            {
                var g = new GUID { Data1 = 0x51EC32B5, Data2 = 0x1B4C, Data3 = 0x453C };
                g.Data4[0] = 0x9C; g.Data4[1] = 0xBD; g.Data4[2] = 0xB6; g.Data4[3] = 0x16;
                g.Data4[4] = 0xBD; g.Data4[5] = 0x62; g.Data4[6] = 0x13; g.Data4[7] = 0x41;
                return g;
            }
        }

        // {5f2a39f5-f14e-4f95-9a9e-b76d568fcf97}
        public static GUID AV1_Profile_Main
        {
            get
            {
                var g = new GUID { Data1 = 0x5F2A39F5, Data2 = 0xF14E, Data3 = 0x4F95 };
                g.Data4[0] = 0x9A; g.Data4[1] = 0x9E; g.Data4[2] = 0xB7; g.Data4[3] = 0x6D;
                g.Data4[4] = 0x56; g.Data4[5] = 0x8F; g.Data4[6] = 0xCF; g.Data4[7] = 0x97;
                return g;
            }
        }

        // =========================================================================================
        // *   Preset GUIDS supported by the NvEncodeAPI interface.
        // =========================================================================================

        // Performance degrades and quality improves as we move from P1 to P7. Presets P3 to P7 for H264 and Presets P2 to P7 for HEVC have B frames enabled by default
        // for HIGH_QUALITY and LOSSLESS tuning info, and will not work with Weighted Prediction enabled. In case Weighted Prediction is required, disable B frames by
        // setting frameIntervalP = 1
        // {FC0A8D3E-5C8B-4A34-A7FB-B7792C1F7F0C}  — P1 (lowest latency)
        public static GUID Preset_P1
        {
            get
            {
                var g = new GUID { Data1 = 0xFC0A8D3E, Data2 = 0x45F8, Data3 = 0x4CF8 };
                g.Data4[0] = 0x80; g.Data4[1] = 0xC7; g.Data4[2] = 0x29; g.Data4[3] = 0x88;
                g.Data4[4] = 0x71; g.Data4[5] = 0x59; g.Data4[6] = 0x0E; g.Data4[7] = 0xBF;
                return g;
            }
        }

        // {F581CFB8-88D6-4381-93F0-DF13F9C27DAB}  — P2
        public static GUID Preset_P2
        {
            get
            {
                var g = new GUID { Data1 = 0xF581CFB8, Data2 = 0x88D6, Data3 = 0x4381 };
                g.Data4[0] = 0x93; g.Data4[1] = 0xF0; g.Data4[2] = 0xDF; g.Data4[3] = 0x13;
                g.Data4[4] = 0xF9; g.Data4[5] = 0xC2; g.Data4[6] = 0x7D; g.Data4[7] = 0xAB;
                return g;
            }
        }

        // {36850110-3A07-441F-94D5-3670631F91F6}
        public static GUID Preset_P3
        {
            get
            {
                var g = new GUID { Data1 = 0x36850110, Data2 = 0x3A07, Data3 = 0x441F };
                g.Data4[0] = 0x94; g.Data4[1] = 0xD5; g.Data4[2] = 0x36; g.Data4[3] = 0x70;
                g.Data4[4] = 0x63; g.Data4[5] = 0x1F; g.Data4[6] = 0x91; g.Data4[7] = 0xF6;
                return g;
            }
        }

        // {90A7B826-DF06-4862-B9D2-CD6D73A08681}
        public static GUID Preset_P4
        {
            get
            {
                var g = new GUID { Data1 = 0x90A7B826, Data2 = 0xDF06, Data3 = 0x4862 };
                g.Data4[0] = 0xB9; g.Data4[1] = 0xD2; g.Data4[2] = 0xCD; g.Data4[3] = 0x6D;
                g.Data4[4] = 0x73; g.Data4[5] = 0xA0; g.Data4[6] = 0x86; g.Data4[7] = 0x81;
                return g;
            }
        }

        // {21C6E6B4-297A-4CBA-998F-B6CBDE72ADE3}
        public static GUID Preset_P5
        {
            get
            {
                var g = new GUID { Data1 = 0x21C6E6B4, Data2 = 0x297A, Data3 = 0x4CBA };
                g.Data4[0] = 0x99; g.Data4[1] = 0x8F; g.Data4[2] = 0xB6; g.Data4[3] = 0xCB;
                g.Data4[4] = 0xDE; g.Data4[5] = 0x72; g.Data4[6] = 0xAD; g.Data4[7] = 0xE3;
                return g;
            }
        }

        // {8E75C279-6299-4AB6-8302-0B215A335CF5}
        public static GUID Preset_P6
        {
            get
            {
                var g = new GUID { Data1 = 0x8E75C279, Data2 = 0x6299, Data3 = 0x4AB6 };
                g.Data4[0] = 0x83; g.Data4[1] = 0x02; g.Data4[2] = 0x0B; g.Data4[3] = 0x21;
                g.Data4[4] = 0x5A; g.Data4[5] = 0x33; g.Data4[6] = 0x5C; g.Data4[7] = 0xF5;
                return g;
            }
        }

        // {84848C12-6F71-4C13-931B-53E283F57974}
        public static GUID Preset_P7
        {
            get
            {
                var g = new GUID { Data1 = 0x84848C12, Data2 = 0x6F71, Data3 = 0x4C13 };
                g.Data4[0] = 0x93; g.Data4[1] = 0x1B; g.Data4[2] = 0x53; g.Data4[3] = 0xE2;
                g.Data4[4] = 0x83; g.Data4[5] = 0xF5; g.Data4[6] = 0x79; g.Data4[7] = 0x74;
                return g;
            }
        }
    }


    /// <summary>
    /// Input struct for querying Encoding capabilities.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_CAPS_PARAM
    {
        /// <summary> [in]: Struct version. Must be set to ::NV_ENC_CAPS_PARAM_VER </summary>
        public uint version;

        /// <summary> [in]: Specifies the encode capability to be queried. Client should pass a member for ::NV_ENC_CAPS enum. </summary>
        public NV_ENC_CAPS capsToQuery;

        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public fixed uint reserved[62];

        public static NV_ENC_CAPS_PARAM Create()
        {
            var config = new NV_ENC_CAPS_PARAM();
            config.version = StructVersion(1);

            return config;
        }
    }

    /// <summary>
    /// Restore encoder state parameters
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_RESTORE_ENCODER_STATE_PARAMS
    {
        /// <summary> [in]: Struct version. </summary>
        public uint version;

        /// <summary> [in]: State buffer index to which the encoder state will be restored </summary>
        public uint bufferIdx;

        /// <summary> [in]: State type to restore </summary>
        public NV_ENC_STATE_RESTORE_TYPE state;

        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public uint reserved;

        /// <summary> 
        /// [in]: Specifies the output buffer pointer, for AV1 encode only. 
        /// Application must call NvEncRestoreEncoderState() API with _NV_ENC_RESTORE_ENCODER_STATE_PARAMS::outputBitstream and 
        /// _NV_ENC_RESTORE_ENCODER_STATE_PARAMS::completionEvent as input when an earlier call to this API returned "NV_ENC_ERR_NEED_MORE_OUTPUT", for AV1 encode.
        /// </summary>
        public void* outputBitstream;

        /// <summary> [in]: Specifies the completion event when asynchronous mode of encoding is enabled. Used for AV1 encode only. </summary>
        public void* completionEvent;

        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public fixed uint reserved1[64];

        /// <summary> [in]: Reserved and must be set to NULL </summary>
        public fixed ulong reserved2[64];

        public static NV_ENC_RESTORE_ENCODER_STATE_PARAMS Create()
        {
            return new NV_ENC_RESTORE_ENCODER_STATE_PARAMS
            {
                version = StructVersion(2)
            };
        }
    }

    /// <summary>
    /// Encoded frame information parameters for every block.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_OUTPUT_STATS_BLOCK
    {
        /// <summary> [in]: Struct version. </summary>
        public uint version;

        /// <summary> [out]: QP of the block </summary>
        public byte QP;

        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public fixed byte reserved[3];

        /// <summary> [out]: Bitcount of the block </summary>
        public uint bitcount;

        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public fixed uint reserved1[13];

        public static NV_ENC_OUTPUT_STATS_BLOCK Create()
        {
            return new NV_ENC_OUTPUT_STATS_BLOCK
            {
                version = StructVersion(1)
            };
        }
    }

    /// <summary>
    /// Encoded frame information parameters for every row.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_OUTPUT_STATS_ROW
    {
        /// <summary> [in]: Struct version. </summary>
        public uint version;

        /// <summary> [out]: QP of the row </summary>
        public byte QP;

        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public fixed byte reserved[3];

        /// <summary> [out]: Bitcount of the row </summary>
        public uint bitcount;

        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public fixed uint reserved1[13];

        public static NV_ENC_OUTPUT_STATS_ROW Create()
        {
            return new NV_ENC_OUTPUT_STATS_ROW
            {
                version = StructVersion(1)
            };
        }
    }

    /// <summary>
    /// Defines a Rectangle. Used in ::NV_ENC_PREPROCESS_FRAME.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NVENC_RECT
    {
        /// <summary>
        /// [in]: X coordinate of the upper left corner of rectangular area to be specified.
        /// </summary>
        public uint left;
        /// <summary>
        /// [in]: Y coordinate of the upper left corner of the rectangular area to be specified.
        /// </summary>
        public uint top;
        /// <summary>
        /// [in]: X coordinate of the bottom right corner of the rectangular area to be specified.
        /// </summary>
        public uint right;
        /// <summary>
        /// [in]: Y coordinate of the bottom right corner of the rectangular area to be specified.
        /// </summary>
        public uint bottom;
    }

    /// <summary>
    /// QP value for frames
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NV_ENC_QP
    {
        /// <summary> [in]: Specifies QP value for P-frame. Even though this field is uint32_t for legacy reasons, the client should treat this as a signed parameter(int32_t) for cases in which negative QP values are to be specified. </summary>
        public uint qpInterP;

        /// <summary> [in]: Specifies QP value for B-frame. Even though this field is uint32_t for legacy reasons, the client should treat this as a signed parameter(int32_t) for cases in which negative QP values are to be specified. </summary>
        public uint qpInterB;

        /// <summary> [in]: Specifies QP value for Intra Frame. Even though this field is uint32_t for legacy reasons, the client should treat this as a signed parameter(int32_t) for cases in which negative QP values are to be specified. </summary>
        public uint qpIntra;
    }

    /// <summary>
    /// Rate Control Configuration Parameters (128 bytes)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_RC_PARAMS
    {
        public uint version;
        public NV_ENC_PARAMS_RC_MODE rateControlMode;
        public NV_ENC_QP constQP;
        public uint averageBitRate;
        public uint maxBitRate;
        public uint vbvBufferSize;
        public uint vbvInitialDelay;


        // --- BITFIELDS ---
        public uint _bitFields;
        public uint enableMinQP { get => (_bitFields & 0x00000001); set => _bitFields = (_bitFields & ~0x00000001u) | (value & 1); }
        public uint enableMaxQP { get => (_bitFields >> 1) & 1; set => _bitFields = (_bitFields & ~(1u << 1)) | ((value & 1) << 1); }
        public uint enableInitialRCQP { get => (_bitFields >> 2) & 1; set => _bitFields = (_bitFields & ~(1u << 2)) | ((value & 1) << 2); }
        public uint enableAQ { get => (_bitFields >> 3) & 1; set => _bitFields = (_bitFields & ~(1u << 3)) | ((value & 1) << 3); }
        public uint reservedBitField1 { get => (_bitFields >> 4) & 1; set => _bitFields = (_bitFields & ~(1u << 4)) | ((value & 1) << 4); }
        public uint enableLookahead { get => (_bitFields >> 5) & 1; set => _bitFields = (_bitFields & ~(1u << 5)) | ((value & 1) << 5); }
        public uint disableIadapt { get => (_bitFields >> 6) & 1; set => _bitFields = (_bitFields & ~(1u << 6)) | ((value & 1) << 6); }
        public uint disableBadapt { get => (_bitFields >> 7) & 1; set => _bitFields = (_bitFields & ~(1u << 7)) | ((value & 1) << 7); }
        public uint enableTemporalAQ { get => (_bitFields >> 8) & 1; set => _bitFields = (_bitFields & ~(1u << 8)) | ((value & 1) << 8); }
        public uint zeroReorderDelay { get => (_bitFields >> 9) & 1; set => _bitFields = (_bitFields & ~(1u << 9)) | ((value & 1) << 9); }
        public uint enableNonRefP { get => (_bitFields >> 10) & 1; set => _bitFields = (_bitFields & ~(1u << 10)) | ((value & 1) << 10); }
        public uint strictGOPTarget { get => (_bitFields >> 11) & 1; set => _bitFields = (_bitFields & ~(1u << 11)) | ((value & 1) << 11); }
        public uint aqStrength { get => (_bitFields >> 12) & 0x0F; set => _bitFields = (_bitFields & ~(0x0Fu << 12)) | ((value & 0x0F) << 12); }
        public uint enableExtLookahead { get => (_bitFields >> 16) & 1; set => _bitFields = (_bitFields & ~(1u << 16)) | ((value & 1) << 16); }
        // bits 17-31 are reservedBitFields:15
        // ------------------------------------------

        public NV_ENC_QP minQP;
        public NV_ENC_QP maxQP;
        public NV_ENC_QP initialRCQP;
        public uint temporallayerIdxMask;
        public unsafe fixed byte temporalLayerQP[8];
        public byte targetQuality;
        public byte targetQualityLSB;
        public ushort lookaheadDepth;
        public byte lowDelayKeyFrameScale;
        public sbyte yDcQPIndexOffset;
        public sbyte uDcQPIndexOffset;
        public sbyte vDcQPIndexOffset;
        public NV_ENC_QP_MAP_MODE qpMapMode;
        public NV_ENC_MULTI_PASS multiPass;
        public uint alphaLayerBitrateRatio;
        public sbyte cbQPIndexOffset;
        public sbyte crQPIndexOffset;
        public ushort reserved2;
        public NV_ENC_LOOKAHEAD_LEVEL lookaheadLevel;
        public unsafe fixed uint reserved[3];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NV_ENC_CLOCK_TIMESTAMP_SET
    {
        private uint _bitFields;

        public uint countingType { get => _bitFields & 1; set => _bitFields = (_bitFields & ~1u) | (value & 1); }
        public uint discontinuityFlag { get => (_bitFields >> 1) & 1; set => _bitFields = (_bitFields & ~(1u << 1)) | ((value & 1) << 1); }
        public uint cntDroppedFrames { get => (_bitFields >> 2) & 1; set => _bitFields = (_bitFields & ~(1u << 2)) | ((value & 1) << 2); }
        public uint nFrames { get => (_bitFields >> 3) & 0xFF; set => _bitFields = (_bitFields & ~(0xFFu << 3)) | ((value & 0xFF) << 3); }
        public uint secondsValue { get => (_bitFields >> 11) & 0x3F; set => _bitFields = (_bitFields & ~(0x3Fu << 11)) | ((value & 0x3F) << 11); }
        public uint minutesValue { get => (_bitFields >> 17) & 0x3F; set => _bitFields = (_bitFields & ~(0x3Fu << 17)) | ((value & 0x3F) << 17); }
        public uint hoursValue { get => (_bitFields >> 23) & 0x1F; set => _bitFields = (_bitFields & ~(0x1Fu << 23)) | ((value & 0x1F) << 23); }
        public uint reserved2 { get => (_bitFields >> 28) & 0xF; set => _bitFields = (_bitFields & ~(0xFu << 28)) | ((value & 0xF) << 28); }

        public uint timeOffset;
    }

    /// <summary>
    /// HEVC encoder configuration parameters to be set during initialization.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_CONFIG_HEVC
    {
        public uint level;
        public uint tier;
        public NV_ENC_HEVC_CUSIZE minCUSize;
        public NV_ENC_HEVC_CUSIZE maxCUSize;

        // --- BITFIELDS ---
        public uint _bitFields;
        public uint useConstrainedIntraPred { get => (_bitFields & 1); set => _bitFields = (_bitFields & ~1u) | (value & 1); }
        public uint disableDeblockAcrossSliceBoundary { get => (_bitFields >> 1) & 1; set => _bitFields = (_bitFields & ~(1u << 1)) | ((value & 1) << 1); }
        public uint outputBufferingPeriodSEI { get => (_bitFields >> 2) & 1; set => _bitFields = (_bitFields & ~(1u << 2)) | ((value & 1) << 2); }
        public uint outputPictureTimingSEI { get => (_bitFields >> 3) & 1; set => _bitFields = (_bitFields & ~(1u << 3)) | ((value & 1) << 3); }
        public uint outputAUD { get => (_bitFields >> 4) & 1; set => _bitFields = (_bitFields & ~(1u << 4)) | ((value & 1) << 4); }
        public uint enableLTR { get => (_bitFields >> 5) & 1; set => _bitFields = (_bitFields & ~(1u << 5)) | ((value & 1) << 5); }
        public uint disableSPSPPS { get => (_bitFields >> 6) & 1; set => _bitFields = (_bitFields & ~(1u << 6)) | ((value & 1) << 6); }
        public uint repeatSPSPPS { get => (_bitFields >> 7) & 1; set => _bitFields = (_bitFields & ~(1u << 7)) | ((value & 1) << 7); }
        public uint enableIntraRefresh { get => (_bitFields >> 8) & 1; set => _bitFields = (_bitFields & ~(1u << 8)) | ((value & 1) << 8); }
        public uint chromaFormatIDC { get => (_bitFields >> 9) & 3; set => _bitFields = (_bitFields & ~(3u << 9)) | ((value & 3) << 9); }
        // bits 11-13 reserved3:3
        public uint enableFillerDataInsertion { get => (_bitFields >> 14) & 1; set => _bitFields = (_bitFields & ~(1u << 14)) | ((value & 1) << 14); }
        public uint enableConstrainedEncoding { get => (_bitFields >> 15) & 1; set => _bitFields = (_bitFields & ~(1u << 15)) | ((value & 1) << 15); }
        public uint enableAlphaLayerEncoding { get => (_bitFields >> 16) & 1; set => _bitFields = (_bitFields & ~(1u << 16)) | ((value & 1) << 16); }
        public uint singleSliceIntraRefresh { get => (_bitFields >> 17) & 1; set => _bitFields = (_bitFields & ~(1u << 17)) | ((value & 1) << 17); }
        public uint outputRecoveryPointSEI { get => (_bitFields >> 18) & 1; set => _bitFields = (_bitFields & ~(1u << 18)) | ((value & 1) << 18); }
        public uint outputTimeCodeSEI { get => (_bitFields >> 19) & 1; set => _bitFields = (_bitFields & ~(1u << 19)) | ((value & 1) << 19); }
        // bits 20-31 reserved:12
        // ----------------------------------------


        public uint idrPeriod;
        public uint intraRefreshPeriod;
        public uint intraRefreshCnt;
        public uint maxNumRefFramesInDPB;
        public uint ltrNumFrames;
        public uint vpsId;
        public uint spsId;
        public uint ppsId;
        public uint sliceMode;
        public uint sliceModeData;
        public uint maxTemporalLayersMinus1;
        public NV_ENC_CONFIG_H264_VUI_PARAMETERS hevcVUIParameters;
        public uint ltrTrustMode;
        public NV_ENC_BFRAME_REF_MODE useBFramesAsRef;
        public NV_ENC_NUM_REF_FRAMES numRefL0;
        public NV_ENC_NUM_REF_FRAMES numRefL1;
        public NV_ENC_TEMPORAL_FILTER_LEVEL tfLevel;
        public uint disableDeblockingFilterIDC;
        public NV_ENC_BIT_DEPTH outputBitDepth;
        public NV_ENC_BIT_DEPTH inputBitDepth;
        public fixed uint reserved1[210];
        public fixed ulong reserved2[64];
    }

    /// <summary>
    /// H264 encoder configuration parameters
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_CONFIG_H264
    {
        // bitFields
        private uint _bitFields1;
        public uint enableTemporalSVC { get => _bitFields1 & 1; set => _bitFields1 = (_bitFields1 & ~1u) | (value & 1); }
        public uint enableStereoMVC { get => (_bitFields1 >> 1) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 1)) | ((value & 1) << 1); }
        public uint hierarchicalPFrames { get => (_bitFields1 >> 2) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 2)) | ((value & 1) << 2); }
        public uint hierarchicalBFrames { get => (_bitFields1 >> 3) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 3)) | ((value & 1) << 3); }
        public uint outputBufferingPeriodSEI { get => (_bitFields1 >> 4) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 4)) | ((value & 1) << 4); }
        public uint outputPictureTimingSEI { get => (_bitFields1 >> 5) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 5)) | ((value & 1) << 5); }
        public uint outputAUD { get => (_bitFields1 >> 6) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 6)) | ((value & 1) << 6); }
        public uint disableSPSPPS { get => (_bitFields1 >> 7) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 7)) | ((value & 1) << 7); }
        public uint outputFramePackingSEI { get => (_bitFields1 >> 8) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 8)) | ((value & 1) << 8); }
        public uint outputRecoveryPointSEI { get => (_bitFields1 >> 9) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 9)) | ((value & 1) << 9); }
        public uint enableIntraRefresh { get => (_bitFields1 >> 10) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 10)) | ((value & 1) << 10); }
        public uint enableConstrainedEncoding { get => (_bitFields1 >> 11) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 11)) | ((value & 1) << 11); }
        public uint repeatSPSPPS { get => (_bitFields1 >> 12) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 12)) | ((value & 1) << 12); }
        public uint enableVFR { get => (_bitFields1 >> 13) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 13)) | ((value & 1) << 13); }
        public uint enableLTR { get => (_bitFields1 >> 14) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 14)) | ((value & 1) << 14); }
        public uint qpPrimeYZeroTransformBypassFlag { get => (_bitFields1 >> 15) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 15)) | ((value & 1) << 15); }
        public uint useConstrainedIntraPred { get => (_bitFields1 >> 16) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 16)) | ((value & 1) << 16); }
        public uint enableFillerDataInsertion { get => (_bitFields1 >> 17) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 17)) | ((value & 1) << 17); }
        public uint disableSVCPrefixNalu { get => (_bitFields1 >> 18) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 18)) | ((value & 1) << 18); }
        public uint enableScalabilityInfoSEI { get => (_bitFields1 >> 19) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 19)) | ((value & 1) << 19); }
        public uint singleSliceIntraRefresh { get => (_bitFields1 >> 20) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 20)) | ((value & 1) << 20); }
        public uint enableTimeCode { get => (_bitFields1 >> 21) & 1; set => _bitFields1 = (_bitFields1 & ~(1u << 21)) | ((value & 1) << 21); }
        public uint reservedBitFields { get => (_bitFields1 >> 22) & 0x3FF; set => _bitFields1 = (_bitFields1 & ~(0x3FFu << 22)) | ((value & 0x3FF) << 22); }
        // end

        public uint level;
        public uint idrPeriod;
        public uint separateColourPlaneFlag;
        public uint disableDeblockingFilterIDC;
        public uint numTemporalLayers;
        public uint spsId;
        public uint ppsId;
        public NV_ENC_H264_ADAPTIVE_TRANSFORM_MODE adaptiveTransformMode;
        public NV_ENC_H264_FMO_MODE fmoMode;
        public NV_ENC_H264_BDIRECT_MODE bdirectMode;
        public NV_ENC_H264_ENTROPY_CODING_MODE entropyCodingMode;
        public NV_ENC_STEREO_PACKING_MODE stereoMode;
        public uint intraRefreshPeriod;
        public uint intraRefreshCnt;
        public uint maxNumRefFrames;
        public uint sliceMode;
        public uint sliceModeData;
        public NV_ENC_CONFIG_H264_VUI_PARAMETERS h264VUIParameters;
        public uint ltrNumFrames;
        public uint ltrTrustMode;
        public uint chromaFormatIDC;
        public uint maxTemporalLayers;
        public NV_ENC_BFRAME_REF_MODE useBFramesAsRef;
        public NV_ENC_NUM_REF_FRAMES numRefL0;
        public NV_ENC_NUM_REF_FRAMES numRefL1;
        public NV_ENC_BIT_DEPTH outputBitDepth;
        public NV_ENC_BIT_DEPTH inputBitDepth;
        public fixed uint reserved1[265];
        public fixed ulong reserved2[64];
    }

    /// <summary>
    /// H264 Video Usability Info (VUI) parameters.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_CONFIG_H264_VUI_PARAMETERS
    {
        /// <summary> [in]: If set to 1 , it specifies that the overscanInfo is present </summary>
        public uint overscanInfoPresentFlag;

        /// <summary> [in]: Specifies the overscan info(as defined in Annex E of the ITU-T Specification). </summary>
        public uint overscanInfo;

        /// <summary> [in]: If set to 1, it specifies  that the videoFormat, videoFullRangeFlag and colourDescriptionPresentFlag are present. </summary>
        public uint videoSignalTypePresentFlag;

        /// <summary> [in]: Specifies the source video format(as defined in Annex E of the ITU-T Specification). </summary>
        public NV_ENC_VUI_VIDEO_FORMAT videoFormat;

        /// <summary> [in]: Specifies the output range of the luma and chroma samples(as defined in Annex E of the ITU-T Specification). </summary>
        public uint videoFullRangeFlag;

        /// <summary> [in]: If set to 1, it specifies that the colourPrimaries, transferCharacteristics and colourMatrix are present. </summary>
        public uint colourDescriptionPresentFlag;

        /// <summary> [in]: Specifies color primaries for converting to RGB(as defined in Annex E of the ITU-T Specification) </summary>
        public NV_ENC_VUI_COLOR_PRIMARIES colourPrimaries;

        /// <summary> [in]: Specifies the opto-electronic transfer characteristics to use (as defined in Annex E of the ITU-T Specification) </summary>
        public NV_ENC_VUI_TRANSFER_CHARACTERISTIC transferCharacteristics;

        /// <summary> [in]: Specifies the matrix coefficients used in deriving the luma and chroma from the RGB primaries (as defined in Annex E of the ITU-T Specification). </summary>
        public NV_ENC_VUI_MATRIX_COEFFS colourMatrix;

        /// <summary> [in]: If set to 1 , it specifies that the chromaSampleLocationTop and chromaSampleLocationBot are present. </summary>
        public uint chromaSampleLocationFlag;

        /// <summary> [in]: Specifies the chroma sample location for top field(as defined in Annex E of the ITU-T Specification) </summary>
        public uint chromaSampleLocationTop;

        /// <summary> [in]: Specifies the chroma sample location for bottom field(as defined in Annex E of the ITU-T Specification) </summary>
        public uint chromaSampleLocationBot;

        /// <summary> [in]: If set to 1, it specifies the bitstream restriction parameters are present in the bitstream. </summary>
        public uint bitstreamRestrictionFlag;

        /// <summary> 
        /// [in]: If set to 1, it specifies that the timingInfo is present and the 'numUnitInTicks' and 'timeScale' fields are specified by the application.
        /// [in]: If not set, the timingInfo may still be present with timing related fields calculated internally basedon the frame rate specified by the application.
        /// </summary>
        public uint timingInfoPresentFlag;

        /// <summary> [in]: Specifies the number of time units of the clock(as defined in Annex E of the ITU-T Specification). </summary>
        public uint numUnitInTicks;

        /// <summary> [in]: Specifies the frquency of the clock(as defined in Annex E of the ITU-T Specification). </summary>
        public uint timeScale;

        /// <summary> Reserved and must be set to 0 </summary>
        public fixed uint reserved[12];
    }

    /// <summary>
    /// AV1 Film Grain Parameters structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct NV_ENC_FILM_GRAIN_PARAMS_AV1
    {
        private uint _bitFields;
        public uint applyGrain { get => _bitFields & 1u; set => _bitFields = (_bitFields & ~1u) | (value & 1u); }
        public uint chromaScalingFromLuma { get => (_bitFields >> 1) & 1u; set => _bitFields = (_bitFields & ~(1u << 1)) | ((value & 1u) << 1); }
        public uint overlapFlag { get => (_bitFields >> 2) & 1u; set => _bitFields = (_bitFields & ~(1u << 2)) | ((value & 1u) << 2); }
        public uint clipToRestrictedRange { get => (_bitFields >> 3) & 1u; set => _bitFields = (_bitFields & ~(1u << 3)) | ((value & 1u) << 3); }
        public uint grainScalingMinus8 { get => (_bitFields >> 4) & 3u; set => _bitFields = (_bitFields & ~(3u << 4)) | ((value & 3u) << 4); }
        public uint arCoeffLag { get => (_bitFields >> 6) & 3u; set => _bitFields = (_bitFields & ~(3u << 6)) | ((value & 3u) << 6); }
        public uint numYPoints { get => (_bitFields >> 8) & 0xFu; set => _bitFields = (_bitFields & ~(0xFu << 8)) | ((value & 0xFu) << 8); }
        public uint numCbPoints { get => (_bitFields >> 12) & 0xFu; set => _bitFields = (_bitFields & ~(0xFu << 12)) | ((value & 0xFu) << 12); }
        public uint numCrPoints { get => (_bitFields >> 16) & 0xFu; set => _bitFields = (_bitFields & ~(0xFu << 16)) | ((value & 0xFu) << 16); }
        public uint arCoeffShiftMinus6 { get => (_bitFields >> 20) & 3u; set => _bitFields = (_bitFields & ~(3u << 20)) | ((value & 3u) << 20); }
        public uint grainScaleShift { get => (_bitFields >> 22) & 3u; set => _bitFields = (_bitFields & ~(3u << 22)) | ((value & 3u) << 22); }
        public uint reserved1 { get => (_bitFields >> 24) & 0xFFu; set => _bitFields = (_bitFields & ~(0xFFu << 24)) | ((value & 0xFFu) << 24); }

        public fixed byte pointYValue[14];
        public fixed byte pointYScaling[14];
        public fixed byte pointCbValue[10];
        public fixed byte pointCbScaling[10];
        public fixed byte pointCrValue[10];
        public fixed byte pointCrScaling[10];
        public fixed byte arCoeffsYPlus128[24];
        public fixed byte arCoeffsCbPlus128[25];
        public fixed byte arCoeffsCrPlus128[25];
        public fixed byte reserved2[2];
        public byte cbMult;
        public byte cbLumaMult;
        public ushort cbOffset;
        public byte crMult;
        public byte crLumaMult;
        public ushort crOffset;
    }

    /// <summary>
    /// AV1 encoder configuration parameters to be set during initialization.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_CONFIG_AV1
    {
        public uint level;
        public uint tier;
        public NV_ENC_AV1_PART_SIZE minPartSize;
        public NV_ENC_AV1_PART_SIZE maxPartSize;
        private uint _bitFields;
        public uint outputAnnexBFormat { get => _bitFields & 1u; set => _bitFields = (_bitFields & ~1u) | (value & 1u); }
        public uint enableTimingInfo { get => (_bitFields >> 1) & 1u; set => _bitFields = (_bitFields & ~(1u << 1)) | ((value & 1u) << 1); }
        public uint enableDecoderModelInfo { get => (_bitFields >> 2) & 1u; set => _bitFields = (_bitFields & ~(1u << 2)) | ((value & 1u) << 2); }
        public uint enableFrameIdNumbers { get => (_bitFields >> 3) & 1u; set => _bitFields = (_bitFields & ~(1u << 3)) | ((value & 1u) << 3); }
        public uint disableSeqHdr { get => (_bitFields >> 4) & 1u; set => _bitFields = (_bitFields & ~(1u << 4)) | ((value & 1u) << 4); }
        public uint repeatSeqHdr { get => (_bitFields >> 5) & 1u; set => _bitFields = (_bitFields & ~(1u << 5)) | ((value & 1u) << 5); }
        public uint enableIntraRefresh { get => (_bitFields >> 6) & 1u; set => _bitFields = (_bitFields & ~(1u << 6)) | ((value & 1u) << 6); }
        public uint chromaFormatIDC { get => (_bitFields >> 7) & 0x3u; set => _bitFields = (_bitFields & ~(0x3u << 7)) | ((value & 0x3u) << 7); }
        public uint enableBitstreamPadding { get => (_bitFields >> 9) & 1u; set => _bitFields = (_bitFields & ~(1u << 9)) | ((value & 1u) << 9); }
        public uint enableCustomTileConfig { get => (_bitFields >> 10) & 1u; set => _bitFields = (_bitFields & ~(1u << 10)) | ((value & 1u) << 10); }
        public uint enableFilmGrainParams { get => (_bitFields >> 11) & 1u; set => _bitFields = (_bitFields & ~(1u << 11)) | ((value & 1u) << 11); }
        public uint reserved4 { get => (_bitFields >> 12) & 0x3Fu; set => _bitFields = (_bitFields & ~(0x3Fu << 12)) | ((value & 0x3Fu) << 12); }
        public uint reserved { get => (_bitFields >> 18) & 0x3FFFu; set => _bitFields = (_bitFields & ~(0x3FFFu << 18)) | ((value & 0x3FFFu) << 18); }

        public uint idrPeriod;
        public uint intraRefreshPeriod;
        public uint intraRefreshCnt;
        public uint maxNumRefFramesInDPB;
        public uint numTileColumns;
        public uint numTileRows;
        public uint reserved2;
        public uint* tileWidths;
        public uint* tileHeights;
        public uint maxTemporalLayersMinus1;
        public NV_ENC_VUI_COLOR_PRIMARIES colorPrimaries;
        public NV_ENC_VUI_TRANSFER_CHARACTERISTIC transferCharacteristics;
        public NV_ENC_VUI_MATRIX_COEFFS matrixCoefficients;
        public uint colorRange;
        public uint chromaSamplePosition;
        public NV_ENC_BFRAME_REF_MODE useBFramesAsRef;
        public void* filmGrainParams;
        public NV_ENC_NUM_REF_FRAMES numFwdRefs;
        public NV_ENC_NUM_REF_FRAMES numBwdRefs;
        public NV_ENC_BIT_DEPTH outputBitDepth;
        public NV_ENC_BIT_DEPTH inputBitDepth;
        public fixed uint reserved1[233];
        public fixed ulong reserved3[62];
    }

    /// <summary>
    /// H264 encoder configuration parameters for ME only Mode
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_CONFIG_H264_MEONLY
    {
        private uint _bitFields;
        public uint disablePartition16x16 { get => _bitFields & 1u; set => _bitFields = (_bitFields & ~1u) | (value & 1u); }
        public uint disablePartition8x16 { get => (_bitFields >> 1) & 1u; set => _bitFields = (_bitFields & ~(1u << 1)) | ((value & 1u) << 1); }
        public uint disablePartition16x8 { get => (_bitFields >> 2) & 1u; set => _bitFields = (_bitFields & ~(1u << 2)) | ((value & 1u) << 2); }
        public uint disablePartition8x8 { get => (_bitFields >> 3) & 1u; set => _bitFields = (_bitFields & ~(1u << 3)) | ((value & 1u) << 3); }
        public uint disableIntraSearch { get => (_bitFields >> 4) & 1u; set => _bitFields = (_bitFields & ~(1u << 4)) | ((value & 1u) << 4); }
        public uint bStereoEnable { get => (_bitFields >> 5) & 1u; set => _bitFields = (_bitFields & ~(1u << 5)) | ((value & 1u) << 5); }
        public uint reservedBitFields { get => (_bitFields >> 6) & 0x3FFFFFFu; set => _bitFields = (_bitFields & ~(0x3FFFFFFu << 6)) | ((value & 0x3FFFFFFu) << 6); }

        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public fixed uint reserved1[255];

        /// <summary> [in]: Reserved and must be set to NULL </summary>
        public fixed ulong reserved2[64];
    }

    /// <summary>
    /// HEVC encoder configuration parameters for ME only Mode
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_CONFIG_HEVC_MEONLY
    {
        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public fixed uint reserved[256];

        /// <summary> [in]: Reserved and must be set to NULL </summary>
        public fixed ulong reserved1[64];
    }

    /// <summary>
    /// Codec-specific encoder configuration parameters to be set during initialization. (UNION)
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 1792)]
    public struct NV_ENC_CODEC_CONFIG
    {
        [FieldOffset(0)]
        public NV_ENC_CONFIG_HEVC hevcConfig;

        [FieldOffset(0)]
        public NV_ENC_CONFIG_H264 h264Config;

        // padding = 1280 bytes
        [FieldOffset(0)]
        public unsafe fixed uint reserved[448];
    }

    /// <summary>
    /// Encoder configuration parameters to be set during initialization.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_CONFIG
    {
        public uint version;
        public GUID profileGUID;
        public uint gopLength;
        public int frameIntervalP;
        public uint monoChromeEncoding;
        public NV_ENC_PARAMS_FRAME_FIELD_MODE frameFieldMode;
        public NV_ENC_MV_PRECISION mvPrecision;
        public NV_ENC_RC_PARAMS rcParams;
        public NV_ENC_CODEC_CONFIG encodeCodecConfig;
        public fixed uint reserved[278];
        public fixed ulong reserved2[64];

        public static NV_ENC_CONFIG Create()
        {
            var config = new NV_ENC_CONFIG();
            config.version = StructVersion(9) | (1u << 31);
            config.rcParams.version = StructVersion(1);

            return config;
        }
    }

    /// <summary>
    /// Encoder preset config
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_PRESET_CONFIG
    {
        public uint version;
        public uint reserved;
        public NV_ENC_CONFIG presetCfg;
        public fixed uint reserved1[256];
        public fixed ulong reserved2[64];

        public static NV_ENC_PRESET_CONFIG Create()
        {
            var config = new NV_ENC_PRESET_CONFIG();
            config.version = StructVersion(5) | (1u << 31);
            config.presetCfg = NV_ENC_CONFIG.Create();

            return config;
        }
    }

    /// <summary>
    /// Event registration/unregistration parameters.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_EVENT_PARAMS
    {
        /// <summary> [in]: Struct version. Must be set to ::NV_ENC_EVENT_PARAMS_VER. </summary>
        public uint version;

        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public uint reserved;

        /// <summary> [in]: Handle to event to be registered/unregistered with the NvEncodeAPI interface. </summary>
        public void* completionEvent;

        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public fixed uint reserved1[254];

        /// <summary> [in]: Reserved and must be set to NULL </summary>
        public fixed ulong reserved2[64];

        public static NV_ENC_EVENT_PARAMS Create()
        {
            return new NV_ENC_EVENT_PARAMS
            {
                version = StructVersion(2)
            };
        }
    }

    /// <summary>
    /// Encoder Session Creation parameters
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS
    {
        public uint version;
        public NV_ENC_DEVICE_TYPE deviceType;
        public void* device;
        public void* reserved;
        public uint apiVersion;
        public fixed uint reserved1[253];
        public fixed ulong reserved2[64];

        public static NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS Create()
        {
            return new NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS
            {
                version = StructVersion(1),
                apiVersion = GetApiVersion(),
            };
        }
    }

    /// <summary>
    /// Encode Session Initialization parameters.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_INITIALIZE_PARAMS
    {
        public uint version;
        public GUID encodeGUID;
        public GUID presetGUID;
        public uint encodeWidth;
        public uint encodeHeight;
        public uint darWidth;
        public uint darHeight;
        public uint frameRateNum;
        public uint frameRateDen;
        public uint enableEncodeAsync;
        public uint enablePTD;

        // --- BITFIELDS ---
        private uint _bitFields;
        public uint reportSliceOffsets { get => _bitFields & 1; set => _bitFields = (_bitFields & ~1u) | (value & 1); }
        public uint enableSubFrameWrite { get => (_bitFields >> 1) & 1; set => _bitFields = (_bitFields & ~(1u << 1)) | ((value & 1) << 1); }
        public uint enableExternalMEHints { get => (_bitFields >> 2) & 1; set => _bitFields = (_bitFields & ~(1u << 2)) | ((value & 1) << 2); }
        public uint enableMEOnlyMode { get => (_bitFields >> 3) & 1; set => _bitFields = (_bitFields & ~(1u << 3)) | ((value & 1) << 3); }
        public uint enableWeightedPrediction { get => (_bitFields >> 4) & 1; set => _bitFields = (_bitFields & ~(1u << 4)) | ((value & 1) << 4); }
        public uint splitEncodeMode { get => (_bitFields >> 5) & 0xF; set => _bitFields = (_bitFields & ~(0xFu << 5)) | ((value & 0xF) << 5); }
        public uint enableOutputInVidmem { get => (_bitFields >> 9) & 1; set => _bitFields = (_bitFields & ~(1u << 9)) | ((value & 1) << 9); }
        public uint enableReconFrameOutput { get => (_bitFields >> 10) & 1; set => _bitFields = (_bitFields & ~(1u << 10)) | ((value & 1) << 10); }
        public uint enableOutputStats { get => (_bitFields >> 11) & 1; set => _bitFields = (_bitFields & ~(1u << 11)) | ((value & 1) << 11); }
        public uint enableUniDirectionalB { get => (_bitFields >> 12) & 1; set => _bitFields = (_bitFields & ~(1u << 12)) | ((value & 1) << 12); }
        // bits 13-31 are reserved
        // ------------------------------------------------

        public uint privDataSize;
        public uint reserved;
        public void* privData;
        public NV_ENC_CONFIG* encodeConfig;
        public uint maxEncodeWidth;
        public uint maxEncodeHeight;
        public NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE maxMEHintCounts0;
        public NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE maxMEHintCounts1;
        public NV_ENC_TUNING_INFO tuningInfo;
        public NV_ENC_BUFFER_FORMAT bufferFormat;
        public uint numStateBuffers;
        public NV_ENC_OUTPUT_STATS_LEVEL outputStatsLevel;
        public fixed uint reserved1[284];
        public fixed ulong reserved2[64];

        public static NV_ENC_INITIALIZE_PARAMS Create()
        {
            var paramsInit = new NV_ENC_INITIALIZE_PARAMS();
            paramsInit.version = StructVersion(7) | (1u << 31);
            paramsInit.enablePTD = 1;

            return paramsInit;
        }
    }

    /// <summary>
    /// Encode Session Reconfigured parameters.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_RECONFIGURE_PARAMS
    {
        /// <summary> [in]: Struct version. Must be set to ::NV_ENC_RECONFIGURE_PARAMS_VER. </summary>
        public uint version;

        /// <summary> [in]: Reserved and must be set to 0 */ </summary>
        public uint reserved;

        /// <summary> 
        /// [in]: Encoder session re-initialization parameters.
        /// If reInitEncodeParams.encodeConfig is NULL and reInitEncodeParams.presetGUID is the same as the preset
        /// GUID specified on the call to NvEncInitializeEncoder(), EncodeAPI will continue to use the existing encode
        /// configuration. If reInitEncodeParams.encodeConfig is NULL and reInitEncodeParams.presetGUID is different from the preset
        /// GUID specified on the call to NvEncInitializeEncoder(), EncodeAPI will try to use the default configuration for
        /// the preset specified by reInitEncodeParams.presetGUID. In this case, reconfiguration may fail if the new
        /// configuration is incompatible with the existing configuration (e.g. the new configuration results in a change in the GOP structure).
        /// </summary>
        public NV_ENC_INITIALIZE_PARAMS reInitEncodeParams;
        private uint _bitFields;

        /// <summary> [in]: Set to 1 to reset the encoder. </summary>
        public uint resetEncoder { get => _bitFields & 1; set => _bitFields = (_bitFields & ~1u) | (value & 1); }

        /// <summary> [in]: Set to 1 to force an IDR frame. </summary>
        public uint forceIDR { get => (_bitFields >> 1) & 1; set => _bitFields = (_bitFields & ~(1u << 1)) | ((value & 1) << 1); }

        /// <summary> [in]: Reserved bitfields (30 bits). </summary>
        public uint reserved1 { get => (_bitFields >> 2) & 0x3FFFFFFF; set => _bitFields = (_bitFields & ~(0x3FFFFFFFu << 2)) | ((value & 0x3FFFFFFF) << 2); }

        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public uint reserved2;

        public static NV_ENC_RECONFIGURE_PARAMS Create()
        {
            var config = new NV_ENC_RECONFIGURE_PARAMS();
            config.version = StructVersion(2) | (1u << 31);
            config.reInitEncodeParams = NV_ENC_INITIALIZE_PARAMS.Create();

            return config;
        }
    }

    /// <summary>
    /// External motion vector hint counts per block type.
    /// H264 and AV1 support multiple hint while HEVC supports one hint for each valid candidate.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE
    {
        private uint _bitFields;

        public uint numCandsPerBlk16x16 { get => _bitFields & 0xF; set => _bitFields = (_bitFields & ~0xFu) | (value & 0xF); }
        public uint numCandsPerBlk16x8 { get => (_bitFields >> 4) & 0xF; set => _bitFields = (_bitFields & ~(0xFu << 4)) | ((value & 0xF) << 4); }
        public uint numCandsPerBlk8x16 { get => (_bitFields >> 8) & 0xF; set => _bitFields = (_bitFields & ~(0xFu << 8)) | ((value & 0xF) << 8); }
        public uint numCandsPerBlk8x8 { get => (_bitFields >> 12) & 0xF; set => _bitFields = (_bitFields & ~(0xFu << 12)) | ((value & 0xF) << 12); }
        public uint numCandsPerSb { get => (_bitFields >> 16) & 0xFF; set => _bitFields = (_bitFields & ~(0xFFu << 16)) | ((value & 0xFF) << 16); }
        public uint reserved { get => (_bitFields >> 24) & 0xFF; set => _bitFields = (_bitFields & ~(0xFFu << 24)) | ((value & 0xFF) << 24); }

        public fixed uint reserved1[3];
    }

    /// <summary>
    /// External Motion Vector hint structure for H264 and HEVC.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NVENC_EXTERNAL_ME_HINT
    {
        private int _bitFields;

        /// <summary> [in]: The x component of MV in quarter-pel units (12 bits) </summary>
        public int mvx { get => (_bitFields << 20) >> 20; set => _bitFields = (_bitFields & ~0xFFF) | (value & 0xFFF); }

        /// <summary> [in]: The y component of MV in quarter-pel units (10 bits) </summary>
        public int mvy { get => (_bitFields << 10) >> 22; set => _bitFields = (_bitFields & ~(0x3FF << 12)) | ((value & 0x3FF) << 12); }

        /// <summary> [in]: Reference index (5 bits) </summary>
        public int refidx { get => (_bitFields >> 22) & 0x1F; set => _bitFields = (_bitFields & ~(0x1F << 22)) | ((value & 0x1F) << 22); }

        /// <summary> [in]: Direction (1 bit) </summary>
        public int dir { get => (_bitFields >> 27) & 0x1; set => _bitFields = (_bitFields & ~(0x1 << 27)) | ((value & 0x1) << 27); }

        /// <summary> [in]: Partition type (2 bits) </summary>
        public int partType { get => (int)(((uint)_bitFields >> 28) & 0x3); set => _bitFields = (int)(((uint)_bitFields & ~(0x3u << 28)) | ((uint)(value & 0x3) << 28)); }

        /// <summary> [in]: Last of partition (1 bit) </summary>
        public int lastofPart { get => (_bitFields >> 30) & 0x1; set => _bitFields = (_bitFields & ~(0x1 << 30)) | ((value & 0x1) << 30); }

        /// <summary> [in]: Last of macroblock (1 bit) </summary>
        public int lastOfMB { get => (int)((uint)_bitFields >> 31); set => _bitFields = (int)(((uint)_bitFields & ~(1u << 31)) | (((uint)value & 0x1u) << 31)); }
    }

    /// <summary>
    /// External Motion Vector SB hint structure for AV1
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NVENC_EXTERNAL_ME_SB_HINT
    {
        private ushort _f0; // refidx(5), direction(1), bi(1), partition_type(3), x8(3), last_of_cu(1), last_of_sb(1), reserved0(1)
        private ushort _f1; // mvx(14), cu_size(2)
        private ushort _f2; // mvy(12), y8(3), reserved1(1)

        public int refidx { get => _f0 & 0x1F; set => _f0 = (ushort)((_f0 & ~0x1F) | (value & 0x1F)); }
        public int direction { get => (_f0 >> 5) & 0x1; set => _f0 = (ushort)((_f0 & ~(1 << 5)) | ((value & 1) << 5)); }
        public int bi { get => (_f0 >> 6) & 0x1; set => _f0 = (ushort)((_f0 & ~(1 << 6)) | ((value & 1) << 6)); }
        public int partition_type { get => (_f0 >> 7) & 0x7; set => _f0 = (ushort)((_f0 & ~(0x7 << 7)) | ((value & 0x7) << 7)); }
        public int x8 { get => (_f0 >> 10) & 0x7; set => _f0 = (ushort)((_f0 & ~(0x7 << 10)) | ((value & 0x7) << 10)); }
        public int last_of_cu { get => (_f0 >> 13) & 0x1; set => _f0 = (ushort)((_f0 & ~(1 << 13)) | ((value & 1) << 13)); }
        public int last_of_sb { get => (_f0 >> 14) & 0x1; set => _f0 = (ushort)((_f0 & ~(1 << 14)) | ((value & 1) << 14)); }
        public int reserved0 { get => (_f0 >> 15) & 0x1; set => _f0 = (ushort)((_f0 & ~(1 << 15)) | ((value & 1) << 15)); }

        public int mvx { get => (short)(_f1 << 2) >> 2; set => _f1 = (ushort)((_f1 & ~0x3FFF) | (value & 0x3FFF)); }
        public int cu_size { get => (_f1 >> 14) & 0x3; set => _f1 = (ushort)((_f1 & ~(0x3 << 14)) | ((value & 0x3) << 14)); }

        public int mvy { get => (short)(_f2 << 4) >> 4; set => _f2 = (ushort)((_f2 & ~0xFFF) | (value & 0xFFF)); }
        public int y8 { get => (_f2 >> 12) & 0x7; set => _f2 = (ushort)((_f2 & ~(0x7 << 12)) | ((value & 0x7) << 12)); }
        public int reserved1 { get => (_f2 >> 15) & 0x1; set => _f2 = (ushort)((_f2 & ~(1 << 15)) | ((value & 1) << 15)); }
    }

    /// <summary>
    /// Register a resource for future use with the Nvidia Video Encoder Interface.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_REGISTER_RESOURCE
    {
        public uint version;
        public NV_ENC_INPUT_RESOURCE_TYPE resourceType;
        public uint width;
        public uint height;
        public uint pitch;
        public uint subResourceIndex;
        public void* resourceToRegister;
        public void* registeredResource;
        public NV_ENC_BUFFER_FORMAT bufferFormat;
        public NV_ENC_BUFFER_USAGE bufferUsage;
        public void* pInputFencePoint;
        public fixed uint chromaOffset[2];
        public fixed uint reserved1[246];
        public fixed ulong reserved2[61];

        public static NV_ENC_REGISTER_RESOURCE Create()
        {
            var res = new NV_ENC_REGISTER_RESOURCE();
            res.version = StructVersion(5);
            res.bufferUsage = NV_ENC_BUFFER_USAGE.NV_ENC_INPUT_IMAGE;

            return res;
        }
    }

    /// <summary>
    /// Encode Stats structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_STAT
    {
        /// <summary> [in]:  Struct version. Must be set to ::NV_ENC_STAT_VER. </summary>
        public uint version;

        /// <summary> [in]:  Reserved and must be set to 0 </summary>
        public uint reserved;

        /// <summary> [in]: Specifies the pointer to output bitstream. </summary>
        public void* outputBitStream;

        /// <summary> [out]: Size of generated bitstream in bytes. </summary>
        public uint bitStreamSize;

        /// <summary> [out]: Picture type of encoded picture. See ::NV_ENC_PIC_TYPE. </summary>
        public uint picType;

        /// <summary> [out]: Offset of last valid bytes of completed bitstream </summary>
        public uint lastValidByteOffset;

        /// <summary> [out]: Offsets of each slice </summary>
        public fixed uint sliceOffsets[16];

        /// <summary> [out]: Picture number </summary>
        public uint picIdx;

        /// <summary> [out]: Average QP of the frame. </summary>
        public uint frameAvgQP;
        private uint _bitFields;

        /// <summary> [out]: Flag indicating this frame is marked as LTR frame </summary>
        public uint ltrFrame { get => _bitFields & 1; set => _bitFields = (_bitFields & ~1u) | (value & 1); }

        /// <summary> [in]:  Reserved bitfields and must be set to 0 </summary>
        public uint reservedBitFields { get => (_bitFields >> 1) & 0x7FFFFFFF; set => _bitFields = (_bitFields & ~(0x7FFFFFFFu << 1)) | ((value & 0x7FFFFFFF) << 1); }

        /// <summary> [out]: Frame index associated with this LTR frame. </summary>
        public uint ltrFrameIdx;

        /// <summary> [out]: For H264, Number of Intra MBs in the encoded frame. For HEVC, Number of Intra CTBs in the encoded frame. </summary>
        public uint intraMBCount;

        /// <summary> [out]: For H264, Number of Inter MBs in the encoded frame, includes skip MBs. For HEVC, Number of Inter CTBs in the encoded frame. </summary>
        public uint interMBCount;

        /// <summary> [out]: Average Motion Vector in X direction for the encoded frame. </summary>
        public int averageMVX;

        /// <summary> [out]: Average Motion Vector in y direction for the encoded frame. </summary>
        public int averageMVY;

        /// <summary> [in]:  Reserved and must be set to 0 </summary>
        public fixed uint reserved1[227];

        /// <summary> [in]:  Reserved and must be set to NULL </summary>
        public fixed ulong reserved2[64];

        public static NV_ENC_STAT Create()
        {
            return new NV_ENC_STAT
            {
                version = StructVersion(2)
            };
        }
    }

    /// <summary>
    /// Sequence and picture paramaters payload.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_SEQUENCE_PARAM_PAYLOAD
    {
        /// <summary> [in]:  Struct version. Must be set to ::NV_ENC_INITIALIZE_PARAMS_VER. </summary>
        public uint version;

        /// <summary> [in]:  Specifies the size of the spsppsBuffer provided by the client </summary>
        public uint inBufferSize;

        /// <summary> [in]:  Specifies the SPS id to be used in sequence header. Default value is 0. </summary>
        public uint spsId;

        /// <summary> [in]:  Specifies the PPS id to be used in picture header. Default value is 0. </summary>
        public uint ppsId;

        /// <summary> 
        /// Specifies bitstream header pointer of size NV_ENC_SEQUENCE_PARAM_PAYLOAD::inBufferSize.
        /// It is the client's responsibility to manage this memory.
        /// </summary>
        public void* spsppsBuffer;

        /// <summary> [out]: Size of the sequence and picture header in bytes. </summary>
        public uint* outSPSPPSPayloadSize;

        /// <summary> [in]:  Reserved and must be set to 0  </summary>
        public fixed uint reserved[250];

        /// <summary> [in]:  Reserved and must be set to NULL </summary>
        public fixed ulong reserved2[64];

        public static NV_ENC_SEQUENCE_PARAM_PAYLOAD Create()
        {
            return new NV_ENC_SEQUENCE_PARAM_PAYLOAD
            {
                version = StructVersion(1)
            };
        }
    }

    /// <summary>
    /// Map an input resource to a Nvidia Encoder Input Buffer
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_MAP_INPUT_RESOURCE
    {
        /// <summary> [in]:  Struct version. Must be set to ::NV_ENC_MAP_INPUT_RESOURCE_VER. </summary>
        public uint version;

        /// <summary> [in]:  Deprecated. Do not use. </summary>
        public uint subResourceIndex;

        /// <summary> [in]:  Deprecated. Do not use. </summary>
        public void* inputResource;

        /// <summary> [in]:  The Registered resource handle obtained by calling NvEncRegisterInputResource. </summary>
        public void* registeredResource;

        /// <summary> [out]: Mapped pointer corresponding to the registeredResource. This pointer must be used in NV_ENC_PIC_PARAMS::inputBuffer parameter in ::NvEncEncodePicture() API. </summary>
        public void* mappedResource;

        /// <summary> [out]: Buffer format of the outputResource. This buffer format must be used in NV_ENC_PIC_PARAMS::bufferFmt if client using the above mapped resource pointer. </summary>
        public NV_ENC_BUFFER_FORMAT mappedBufferFmt;

        /// <summary> [in]:  Reserved and must be set to 0. </summary>
        public fixed uint reserved1[251];

        /// <summary> [in]:  Reserved and must be set to NULL </summary>
        public fixed ulong reserved2[63];

        public static NV_ENC_MAP_INPUT_RESOURCE Create()
        {
            var mapRes = new NV_ENC_MAP_INPUT_RESOURCE();
            mapRes.version = StructVersion(4);

            return mapRes;
        }
    }

    /// <summary>
    /// NV_ENC_REGISTER_RESOURCE::resourceToRegister must be a pointer to a variable of this type,
    /// when NV_ENC_REGISTER_RESOURCE::resourceType is NV_ENC_INPUT_RESOURCE_TYPE_OPENGL_TEX
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_INPUT_RESOURCE_OPENGL_TEX
    {
        /// <summary> [in]: The name of the texture to be used. </summary>
        public uint texture;

        /// <summary> [in]: Accepted values are GL_TEXTURE_RECTANGLE and GL_TEXTURE_2D. </summary>
        public uint target;
    }

    /// <summary>
    /// Fence and fence value for synchronization.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_FENCE_POINT_D3D12
    {
        /// <summary> [in]: Struct version. Must be set to ::NV_ENC_FENCE_POINT_D3D12_VER. </summary>
        public uint version;

        /// <summary> [in]: Reserved and must be set to 0. </summary>
        public uint reserved;

        /// <summary> [in]: Pointer to ID3D12Fence. This fence object is used for synchronization. </summary>
        public void* pFence;

        /// <summary> [in]: Fence value to reach or exceed before the GPU operation. </summary>
        public ulong waitValue;

        /// <summary> [in]: Fence value to set the fence to, after the GPU operation. </summary>
        public ulong signalValue;
        private uint _bitFields;

        /// <summary> [in]: Wait on 'waitValue' if bWait is set to 1, before starting GPU operation. </summary>
        public uint bWait { get => _bitFields & 1u; set => _bitFields = (_bitFields & ~1u) | (value & 1u); }

        /// <summary> [in]: Signal on 'signalValue' if bSignal is set to 1, after GPU operation is complete. </summary>
        public uint bSignal { get => (_bitFields >> 1) & 1u; set => _bitFields = (_bitFields & ~(1u << 1)) | ((value & 1u) << 1); }

        /// <summary> [in]: Reserved and must be set to 0. </summary>
        public uint reservedBitField { get => (_bitFields >> 2) & 0x3FFFFFFFu; set => _bitFields = (_bitFields & ~(0x3FFFFFFFu << 2)) | ((value & 0x3FFFFFFFu) << 2); }

        /// <summary> [in]: Reserved and must be set to 0. </summary>
        public fixed uint reserved1[7];

        public static NV_ENC_FENCE_POINT_D3D12 Create()
        {
            return new NV_ENC_FENCE_POINT_D3D12
            {
                version = StructVersion(1)
            };
        }
    }

    /// <summary>
    /// NV_ENC_PIC_PARAMS::inputBuffer and NV_ENC_PIC_PARAMS::alphaBuffer must be a pointer to a struct of this type, when D3D12 interface is used
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_INPUT_RESOURCE_D3D12
    {
        /// <summary> [in]: Struct version. Must be set to ::NV_ENC_INPUT_RESOURCE_D3D12_VER. </summary>
        public uint version;

        /// <summary> [in]: Reserved and must be set to 0. </summary>
        public uint reserved;

        /// <summary>
        /// [in]: Specifies the input surface pointer. Client must use a pointer obtained from NvEncMapInputResource() in NV_ENC_MAP_INPUT_RESOURCE::mappedResource
        /// when mapping the input surface.
        /// </summary>
        public void* pInputBuffer;

        /// <summary> [in]: Specifies the fence and corresponding fence values to do GPU wait and signal. </summary>
        public NV_ENC_FENCE_POINT_D3D12 inputFencePoint;

        /// <summary> [in]: Reserved and must be set to 0. </summary>
        public fixed uint reserved1[16];

        /// <summary> [in]: Reserved and must be set to NULL. </summary>
        public fixed ulong reserved2[16];

        public static NV_ENC_INPUT_RESOURCE_D3D12 Create()
        {
            return new NV_ENC_INPUT_RESOURCE_D3D12
            {
                version = StructVersion(1)
            };
        }
    }

    /// <summary>
    /// NV_ENC_PIC_PARAMS::outputBitstream and NV_ENC_LOCK_BITSTREAM::outputBitstream must be a pointer to a struct of this type, when D3D12 interface is used
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_OUTPUT_RESOURCE_D3D12
    {
        /// <summary> [in]: Struct version. Must be set to ::NV_ENC_OUTPUT_RESOURCE_D3D12_VER. </summary>
        public uint version;

        /// <summary> [in]: Reserved and must be set to 0. </summary>
        public uint reserved;

        /// <summary>
        /// [in]: Specifies the output buffer pointer. Client must use a pointer obtained from NvEncMapInputResource() in NV_ENC_MAP_INPUT_RESOURCE::mappedResource
        /// when mapping output bitstream buffer
        /// </summary>
        public void* pOutputBuffer;

        /// <summary> [in]: Specifies the fence and corresponding fence values to do GPU wait and signal. </summary>
        public NV_ENC_FENCE_POINT_D3D12 outputFencePoint;

        /// <summary> [in]: Reserved and must be set to 0. </summary>
        public fixed uint reserved1[16];

        /// <summary> [in]: Reserved and must be set to NULL. </summary>
        public fixed ulong reserved2[16];

        public static NV_ENC_OUTPUT_RESOURCE_D3D12 Create()
        {
            return new NV_ENC_OUTPUT_RESOURCE_D3D12
            {
                version = StructVersion(5)
            };
        }
    }

    /// <summary>
    /// Encoding parameters that need to be sent on a per frame basis.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_PIC_PARAMS
    {
        public uint version;
        public uint inputWidth;
        public uint inputHeight;
        public uint inputPitch;
        public uint encodePicFlags;
        public uint frameIdx;
        public ulong inputTimeStamp;
        public ulong inputDuration;
        public void* inputBuffer;
        public void* outputBitstream;
        public void* completionEvent;
        public NV_ENC_BUFFER_FORMAT bufferFmt;
        public NV_ENC_PIC_STRUCT pictureStruct;
        public NV_ENC_PIC_TYPE pictureType;
        public NV_ENC_CODEC_PIC_PARAMS codecPicParams;
        public NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE meHintCounts0;
        public NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE meHintCounts1;
        public void* meExternalHints;
        public fixed uint reserved2[7];
        public fixed ulong reserved5[2];
        public sbyte* qpDeltaMap;
        public uint qpDeltaMapSize;
        public uint reservedBitFields;
        public fixed ushort meHintRefPicDist[2];
        public uint reserved4;
        public void* alphaBuffer;
        public void* meExternalSbHints;
        public uint meSbHintsCount;
        public uint stateBufferIdx;
        public void* outputReconBuffer;
        public fixed uint reserved3[284];
        public fixed ulong reserved6[57];

        public static NV_ENC_PIC_PARAMS Create()
        {
            var p = new NV_ENC_PIC_PARAMS();
            p.version = StructVersion(7) | (1u << 31);

            return p;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 1536)]
    public unsafe struct NV_ENC_CODEC_PIC_PARAMS
    {
        [FieldOffset(0)]
        public NV_ENC_PIC_PARAMS_H264 h264PicParams;
        [FieldOffset(0)]
        public NV_ENC_PIC_PARAMS_HEVC hevcPicParams;
        [FieldOffset(0)]
        public NV_ENC_PIC_PARAMS_AV1 av1PicParams;
        [FieldOffset(0)]
        public fixed uint reserved[384];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_PIC_PARAMS_H264
    {
        public uint displayPOCSyntax;
        public uint reserved3;
        public uint refPicFlag;
        public uint colourPlaneId;
        public uint forceIntraRefreshWithFrameCnt;

        private uint _bitFields;
        public uint constrainedFrame { get => _bitFields & 1; set => _bitFields = (_bitFields & ~1u) | (value & 1); }
        public uint sliceModeDataUpdate { get => (_bitFields >> 1) & 1; set => _bitFields = (_bitFields & ~(1u << 1)) | ((value & 1) << 1); }
        public uint ltrMarkFrame { get => (_bitFields >> 2) & 1; set => _bitFields = (_bitFields & ~(1u << 2)) | ((value & 1) << 2); }
        public uint ltrUseFrames { get => (_bitFields >> 3) & 1; set => _bitFields = (_bitFields & ~(1u << 3)) | ((value & 1) << 3); }
        public uint reservedBitFields { get => (_bitFields >> 4) & 0x0FFFFFFF; set => _bitFields = (_bitFields & ~(0x0FFFFFFFu << 4)) | ((value & 0x0FFFFFFF) << 4); }

        public byte* sliceTypeData;
        public uint sliceTypeArrayCnt;
        public uint seiPayloadArrayCnt;
        public void* seiPayloadArray;
        public uint sliceMode;
        public uint sliceModeData;
        public uint ltrMarkFrameIdx;
        public uint ltrUseFrameBitmap;
        public uint ltrUsageMode;
        public uint forceIntraSliceCount;
        public void* forceIntraSliceIdx;
        public NV_ENC_PIC_PARAMS_H264_EXT h264ExtPicParams;
        public NV_ENC_TIME_CODE timeCode;
        public fixed uint reserved1[202];
        public fixed ulong reserved2[61];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_PIC_PARAMS_HEVC
    {
        public uint displayPOCSyntax;
        public uint refPicFlag;
        public uint temporalId;
        public uint forceIntraRefreshWithFrameCnt;

        private uint _bitFields;
        public uint constrainedFrame { get => _bitFields & 1; set => _bitFields = (_bitFields & ~1u) | (value & 1); }
        public uint sliceModeDataUpdate { get => (_bitFields >> 1) & 1; set => _bitFields = (_bitFields & ~(1u << 1)) | ((value & 1) << 1); }
        public uint ltrMarkFrame { get => (_bitFields >> 2) & 1; set => _bitFields = (_bitFields & ~(1u << 2)) | ((value & 1) << 2); }
        public uint ltrUseFrames { get => (_bitFields >> 3) & 1; set => _bitFields = (_bitFields & ~(1u << 3)) | ((value & 1) << 3); }
        public uint reservedBitFields { get => (_bitFields >> 4) & 0x0FFFFFFF; set => _bitFields = (_bitFields & ~(0x0FFFFFFFu << 4)) | ((value & 0x0FFFFFFF) << 4); }

        public uint reserved1;
        public byte* sliceTypeData;
        public uint sliceTypeArrayCnt;
        public uint sliceMode;
        public uint sliceModeData;
        public uint ltrMarkFrameIdx;
        public uint ltrUseFrameBitmap;
        public uint ltrUsageMode;
        public uint seiPayloadArrayCnt;
        public uint reserved;
        public void* seiPayloadArray;
        public NV_ENC_TIME_CODE timeCode;

        public fixed uint reserved2[236];
        public fixed ulong reserved3[61];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_PIC_PARAMS_AV1
    {
        public uint displayPOCSyntax;
        public uint refPicFlag;
        public uint temporalId;
        public uint forceIntraRefreshWithFrameCnt;

        private uint _bitFields;
        public uint goldenFrameFlag { get => _bitFields & 1; set => _bitFields = (_bitFields & ~1u) | (value & 1); }
        public uint arfFrameFlag { get => (_bitFields >> 1) & 1; set => _bitFields = (_bitFields & ~(1u << 1)) | ((value & 1) << 1); }
        public uint arf2FrameFlag { get => (_bitFields >> 2) & 1; set => _bitFields = (_bitFields & ~(1u << 2)) | ((value & 1) << 2); }
        public uint bwdFrameFlag { get => (_bitFields >> 3) & 1; set => _bitFields = (_bitFields & ~(1u << 3)) | ((value & 1) << 3); }
        public uint overlayFrameFlag { get => (_bitFields >> 4) & 1; set => _bitFields = (_bitFields & ~(1u << 4)) | ((value & 1) << 4); }
        public uint showExistingFrameFlag { get => (_bitFields >> 5) & 1; set => _bitFields = (_bitFields & ~(1u << 5)) | ((value & 1) << 5); }
        public uint errorResilientModeFlag { get => (_bitFields >> 6) & 1; set => _bitFields = (_bitFields & ~(1u << 6)) | ((value & 1) << 6); }
        public uint tileConfigUpdate { get => (_bitFields >> 7) & 1; set => _bitFields = (_bitFields & ~(1u << 7)) | ((value & 1) << 7); }
        public uint enableCustomTileConfig { get => (_bitFields >> 8) & 1; set => _bitFields = (_bitFields & ~(1u << 8)) | ((value & 1) << 8); }
        public uint filmGrainParamsUpdate { get => (_bitFields >> 9) & 1; set => _bitFields = (_bitFields & ~(1u << 9)) | ((value & 1) << 9); }
        public uint reservedBitFields { get => (_bitFields >> 10) & 0x3FFFFF; set => _bitFields = (_bitFields & ~(0x3FFFFFu << 10)) | ((value & 0x3FFFFF) << 10); }

        public uint numTileColumns;
        public uint numTileRows;
        public uint reserved;
        public uint* tileWidths;
        public uint* tileHeights;
        public uint obuPayloadArrayCnt;
        public uint reserved1;
        public void* obuPayloadArray;
        public void* filmGrainParams;
        public fixed uint reserved2[246];
        public fixed ulong reserved3[61];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NV_ENC_TIME_CODE
    {
        public NV_ENC_DISPLAY_PIC_STRUCT displayPicStruct;
        public NV_ENC_CLOCK_TIMESTAMP_SET clockTimestamp0;
        public NV_ENC_CLOCK_TIMESTAMP_SET clockTimestamp1;
        public NV_ENC_CLOCK_TIMESTAMP_SET clockTimestamp2;
        public uint skipClockTimestampInsertion;
    }

    [StructLayout(LayoutKind.Explicit, Size = 128)]
    public unsafe struct NV_ENC_PIC_PARAMS_H264_EXT
    {
        [FieldOffset(0)]
        public NV_ENC_PIC_PARAMS_MVC mvcPicParams;
        [FieldOffset(0)]
        public fixed uint reserved1[32];
    }

    /// <summary>
    /// User SEI message
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_SEI_PAYLOAD
    {
        /// <summary> [in] SEI payload size in bytes. SEI payload must be byte aligned, as described in Annex D </summary>
        public uint payloadSize;

        /// <summary> [in] SEI payload types and syntax can be found in Annex D of the H.264 Specification. </summary>
        public uint payloadType;

        /// <summary> [in] pointer to user data </summary>
        public void* payload;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_PIC_PARAMS_MVC
    {
        public uint version;
        public uint viewID;
        public uint temporalID;
        public uint priorityID;
        public fixed uint reserved1[12];
        public fixed ulong reserved2[8];

        public static NV_ENC_PIC_PARAMS_MVC Create()
        {
            return new NV_ENC_PIC_PARAMS_MVC
            {
                version = StructVersion(1)
            };
        }
    }

    /// <summary>
    /// MEOnly parameters that need to be sent on a per motion estimation basis.
    /// NV_ENC_MEONLY_PARAMS::meExternalHints is supported for H264 only.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_MEONLY_PARAMS
    {
        public uint version;
        public uint inputWidth;
        public uint inputHeight;
        public uint reserved;
        public void* inputBuffer;
        public void* referenceFrame;
        public void* mvBuffer;
        public uint reserved2;
        public NV_ENC_BUFFER_FORMAT bufferFmt;
        public void* completionEvent;
        public uint viewID;
        public NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE meHintCountsPerBlock0;
        public NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE meHintCountsPerBlock1;
        public void* meExternalHints;
        public fixed uint reserved1[241];
        public fixed ulong reserved3[59];

        public static NV_ENC_MEONLY_PARAMS Create()
        {
            return new NV_ENC_MEONLY_PARAMS
            {
                version = StructVersion(4)
            };
        }
    }

    /// <summary>
    /// Bitstream buffer lock parameters.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_LOCK_BITSTREAM
    {
        public uint version;

        private uint _bitFields;
        public uint doNotWait { get => _bitFields & 1; set => _bitFields = (_bitFields & ~1u) | (value & 1); }
        public uint ltrFrame { get => (_bitFields >> 1) & 1; set => _bitFields = (_bitFields & ~(1u << 1)) | ((value & 1) << 1); }
        public uint getRCStats { get => (_bitFields >> 2) & 1; set => _bitFields = (_bitFields & ~(1u << 2)) | ((value & 1) << 2); }
        public uint reservedBitFields { get => (_bitFields >> 3) & 0x1FFFFFFF; set => _bitFields = (_bitFields & ~(0x1FFFFFFFu << 3)) | ((value & 0x1FFFFFFF) << 3); }

        public void* outputBitstream;
        public void* sliceOffsets;
        public uint frameIdx;
        public uint hwEncodeStatus;
        public uint numSlices;
        public uint bitstreamSizeInBytes;
        public ulong outputTimeStamp;
        public ulong outputDuration;
        public void* bitstreamBufferPtr;
        public NV_ENC_PIC_TYPE pictureType;
        public NV_ENC_PIC_STRUCT pictureStruct;
        public uint frameAvgQP;
        public uint frameSatd;
        public uint ltrFrameIdx;
        public uint ltrFrameBitmap;
        public uint temporalId;
        public uint intraMBCount;
        public uint interMBCount;
        public int averageMVX;
        public int averageMVY;
        public uint alphaLayerSizeInBytes;
        public uint outputStatsPtrSize;
        public uint reserved;
        public void* outputStatsPtr;
        public uint frameIdxDisplay;
        public fixed uint reserved1[219];
        public fixed ulong reserved2[63];
        public fixed uint reservedInternal[8];

        public static NV_ENC_LOCK_BITSTREAM Create()
        {
            return new NV_ENC_LOCK_BITSTREAM
            {
                version = StructVersion(2) | (1u << 31)
            };
        }
    }

    /// <summary>
    /// Uncompressed Input Buffer lock parameters.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_LOCK_INPUT_BUFFER
    {
        /// <summary> [in]:  Struct version. Must be set to ::NV_ENC_LOCK_INPUT_BUFFER_VER. </summary>
        public uint version;
        private uint _bitFields;

        /// <summary> [in]:  Set to 1 to make ::NvEncLockInputBuffer() a unblocking call. If the encoding is not completed, driver will return ::NV_ENC_ERR_ENCODER_BUSY error code. </summary>
        public uint doNotWait { get => _bitFields & 1; set => _bitFields = (_bitFields & ~1u) | (value & 1); }

        /// <summary> [in]:  Reserved bitfields and must be set to 0 </summary>
        public uint reservedBitFields { get => (_bitFields >> 1) & 0x7FFFFFFF; set => _bitFields = (_bitFields & ~(0x7FFFFFFFu << 1)) | ((value & 0x7FFFFFFF) << 1); }

        /// <summary> [in]:  Pointer to the input buffer to be locked, client should pass the pointer obtained from ::NvEncCreateInputBuffer() or ::NvEncMapInputResource API. </summary>
        public void* inputBuffer;

        /// <summary> [out]: Pointed to the locked input buffer data. Client can only access input buffer using the \p bufferDataPtr. </summary>
        public void* bufferDataPtr;

        /// <summary> [out]: Pitch of the locked input buffer. </summary>
        public uint pitch;

        /// <summary> [in]:  Reserved and must be set to 0 </summary>
        public fixed uint reserved1[251];

        /// <summary> [in]:  Reserved and must be set to NULL </summary>
        public fixed ulong reserved2[64];

        public static NV_ENC_LOCK_INPUT_BUFFER Create()
        {
            return new NV_ENC_LOCK_INPUT_BUFFER
            {
                version = StructVersion(1)
            };
        }
    }

    /// <summary>
    /// Encoder Output parameters
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_ENCODE_OUT_PARAMS
    {
        /// <summary> [out]: Struct version. </summary>
        public uint version;

        /// <summary> [out]: Encoded bitstream size in bytes </summary>
        public uint bitstreamSizeInBytes;

        /// <summary> [out]: Reserved and must be set to 0 </summary>
        public fixed uint reserved[62];

        public static NV_ENC_ENCODE_OUT_PARAMS Create()
        {
            return new NV_ENC_ENCODE_OUT_PARAMS
            {
                version = StructVersion(1)
            };
        }
    }

    /// <summary>
    /// Lookahead picture parameters
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_LOOKAHEAD_PIC_PARAMS
    {
        /// <summary> [in]: Struct version. </summary>
        public uint version;

        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public uint reserved;

        /// <summary> [in]: Specifies the input buffer pointer. Client must use a pointer obtained from ::NvEncCreateInputBuffer() or ::NvEncMapInputResource() APIs. </summary>
        public void* inputBuffer;

        /// <summary> [in]: Specifies input picture type. Client required to be set explicitly by the client if the client has not set NV_ENC_INITALIZE_PARAMS::enablePTD to 1 while calling NvInitializeEncoder. </summary>
        public NV_ENC_PIC_TYPE pictureType;

        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public fixed uint reserved1[63];

        /// <summary> [in]: Reserved and must be set to NULL </summary>
        public fixed ulong reserved2[64];

        public static NV_ENC_LOOKAHEAD_PIC_PARAMS Create()
        {
            return new NV_ENC_LOOKAHEAD_PIC_PARAMS
            {
                version = StructVersion(2)
            };
        }
    }

    /// <summary>
    /// Creation parameters for input buffer.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_CREATE_INPUT_BUFFER
    {
        /// <summary> [in]: Struct version. Must be set to ::NV_ENC_CREATE_INPUT_BUFFER_VER </summary>
        public uint version;

        /// <summary> [in]: Input frame width </summary>
        public uint width;

        /// <summary> [in]: Input frame height </summary>
        public uint height;

        /// <summary> [in]: Deprecated. Do not use </summary>
        public NV_ENC_MEMORY_HEAP memoryHeap;

        /// <summary> [in]: Input buffer format </summary>
        public NV_ENC_BUFFER_FORMAT bufferFmt;

        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public uint reserved;

        /// <summary> [out]: Pointer to input buffer </summary>
        public void* inputBuffer;

        /// <summary> [in]: Pointer to existing system memory buffer </summary>
        public void* pSysMemBuffer;

        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public fixed uint reserved1[58];

        /// <summary> [in]: Reserved and must be set to NULL </summary>
        public fixed ulong reserved2[63];

        public static NV_ENC_CREATE_INPUT_BUFFER Create()
        {
            return new NV_ENC_CREATE_INPUT_BUFFER
            {
                version = StructVersion(2)
            };
        }
    }

    /// <summary>
    /// Creation parameters for output bitstream buffer.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_CREATE_BITSTREAM_BUFFER
    {
        /// <summary> [in]: Struct version. Must be set to ::NV_ENC_CREATE_BITSTREAM_BUFFER_VER </summary>
        public uint version;

        /// <summary> [in]: Deprecated. Do not use </summary>
        public uint size;

        /// <summary> [in]: Deprecated. Do not use </summary>
        public NV_ENC_MEMORY_HEAP memoryHeap;

        /// <summary> [in]: Reserved and must be set to 0 </summary>
        public uint reserved;

        /// <summary> [out]: Pointer to the output bitstream buffer </summary>
        public void* bitstreamBuffer;

        /// <summary> [out]: Reserved and should not be used </summary>
        public void* bitstreamBufferPtr;

        /// <summary> [in]: Reserved and should be set to 0 </summary>
        public fixed uint reserved1[58];

        /// <summary> [in]: Reserved and should be set to NULL </summary>
        public fixed ulong reserved2[64];

        public static NV_ENC_CREATE_BITSTREAM_BUFFER Create()
        {
            return new NV_ENC_CREATE_BITSTREAM_BUFFER
            {
                version = StructVersion(1)
            };
        }
    }

    /// <summary>
    /// Structs needed for ME only mode.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_MVECTOR
    {
        /// <summary> the x component of MV in quarter-pel units </summary>
        public short mvx;

        /// <summary> the y component of MV in quarter-pel units </summary>
        public short mvy;
    }

    /// <summary>
    /// Motion vector structure per macroblock for H264 motion estimation.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_H264_MV_DATA
    {
        /// <summary> up to 4 vectors for 8x8 partition </summary>
        public NV_ENC_MVECTOR mv0;
        public NV_ENC_MVECTOR mv1;
        public NV_ENC_MVECTOR mv2;
        public NV_ENC_MVECTOR mv3;

        /// <summary> 0 (I), 1 (P), 2 (IPCM), 3 (B) </summary>
        public byte mbType;

        /// <summary> up to 4 vectors for 8x8 partition </summary>
        public byte partitionType;

        /// <summary> Specifies the block partition type. 0:16x16, 1:8x8, 2:16x8, 3:8x16 </summary>
        public ushort reserved;

        /// <summary> reserved padding for alignment </summary>
        public uint mbCost;
    }

    /// <summary>
    /// Motion vector structure per CU for HEVC motion estimation.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_HEVC_MV_DATA
    {
        /// <summary> up to 4 vectors within a CU </summary>
        public NV_ENC_MVECTOR mv0;
        public NV_ENC_MVECTOR mv1;
        public NV_ENC_MVECTOR mv2;
        public NV_ENC_MVECTOR mv3;

        /// <summary> 0 (I), 1(P) </summary>
        public byte cuType;

        /// <summary> 0: 8x8, 1: 16x16, 2: 32x32, 3: 64x64 </summary>
        public byte cuSize;

        /// <summary> The CU partition mode
        /// 0 (2Nx2N), 1 (2NxN), 2(Nx2N), 3 (NxN),
        /// 4 (2NxnU), 5 (2NxnD), 6(nLx2N), 7 (nRx2N) 
        /// </summary>
        public byte partitionMode;

        /// <summary> Marker to separate CUs in the current CTB from CUs in the next CTB </summary>
        public byte lastCUInCTB;
    }

    /// <summary>
    /// Creation parameters for output motion vector buffer for ME only mode.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENC_CREATE_MV_BUFFER
    {
        /// <summary> [in]: Struct version. Must be set to NV_ENC_CREATE_MV_BUFFER_VER </summary>
        public uint version;

        /// <summary> [in]: Reserved and should be set to 0 </summary>
        public uint reserved;

        /// <summary> [out]: Pointer to the output motion vector buffer </summary>
        public void* mvBuffer;

        /// <summary> [in]: Reserved and should be set to 0 </summary>
        public fixed uint reserved1[254];

        /// <summary> [in]: Reserved and should be set to NULL </summary>
        public fixed ulong reserved2[63];

        public static NV_ENC_CREATE_MV_BUFFER Create()
        {
            return new NV_ENC_CREATE_MV_BUFFER
            {
                version = StructVersion(2)
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // NVENC Function Pointers
    // ═══════════════════════════════════════════════════════════════

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate NVENCSTATUS NvEncOpenEncodeSessionEx(
        NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS* openSessionExParams,
        void** encoder);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate NVENCSTATUS NvEncGetEncodePresetConfigEx(
        void* encoder,
        GUID encodeGUID,
        GUID presetGUID,
        NV_ENC_TUNING_INFO tuningInfo,
        NV_ENC_PRESET_CONFIG* presetConfig);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate NVENCSTATUS NvEncInitializeEncoder(
        void* encoder,
        NV_ENC_INITIALIZE_PARAMS* createEncodeParams);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate NVENCSTATUS NvEncRegisterResource(
        void* encoder,
        NV_ENC_REGISTER_RESOURCE* registerResParams);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate NVENCSTATUS NvEncMapInputResource(
        void* encoder,
        NV_ENC_MAP_INPUT_RESOURCE* mapInputResParams);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate NVENCSTATUS NvEncUnmapInputResource(
        void* encoder,
        void* mappedInputBuffer);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate NVENCSTATUS NvEncUnregisterResource(
        void* encoder,
        void* registeredResource);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate NVENCSTATUS NvEncCreateBitstreamBuffer(
        void* encoder,
        NV_ENC_CREATE_BITSTREAM_BUFFER* createBitstreamBufferParams);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate NVENCSTATUS NvEncEncodePicture(
        void* encoder,
        NV_ENC_PIC_PARAMS* encodePicParams);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate NVENCSTATUS NvEncLockBitstream(
        void* encoder,
        NV_ENC_LOCK_BITSTREAM* lockBitstreamBufferParams);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate NVENCSTATUS NvEncUnlockBitstream(
        void* encoder,
        void* bitstreamBuffer);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate NVENCSTATUS NvEncDestroyBitstreamBuffer(
        void* encoder,
        void* bitstreamBuffer);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate NVENCSTATUS NvEncDestroyEncoder(void* encoder);

    // ═══════════════════════════════════════════════════════════════
    // NV_ENCODE_API_FUNCTION_LIST
    // Slot order must match nvEncodeAPI.h EXACTLY — any wrong offset
    // means you call a completely different function.
    // ═══════════════════════════════════════════════════════════════

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NV_ENCODE_API_FUNCTION_LIST
    {
        public uint version;
        public uint reserved;
        public void* nvEncOpenEncodeSession;
        public void* nvEncGetEncodeGUIDCount;
        public void* nvEncGetEncodeProfileGUIDCount;
        public void* nvEncGetEncodeProfileGUIDs;
        public void* nvEncGetEncodeGUIDs;
        public void* nvEncGetInputFormatCount;
        public void* nvEncGetInputFormats;
        public void* nvEncGetEncodeCaps;
        public void* nvEncGetEncodePresetCount;
        public void* nvEncGetEncodePresetGUIDs;
        public void* nvEncGetEncodePresetConfig;
        public void* nvEncInitializeEncoder;
        public void* nvEncCreateInputBuffer;
        public void* nvEncDestroyInputBuffer;
        public void* nvEncCreateBitstreamBuffer;
        public void* nvEncDestroyBitstreamBuffer;
        public void* nvEncEncodePicture;
        public void* nvEncLockBitstream;
        public void* nvEncUnlockBitstream;
        public void* nvEncLockInputBuffer;
        public void* nvEncUnlockInputBuffer;
        public void* nvEncGetEncodeStats;
        public void* nvEncGetSequenceParams;
        public void* nvEncRegisterAsyncEvent;
        public void* nvEncUnregisterAsyncEvent;
        public void* nvEncMapInputResource;
        public void* nvEncUnmapInputResource;
        public void* nvEncDestroyEncoder;
        public void* nvEncInvalidateRefFrames;
        public void* nvEncOpenEncodeSessionEx;
        public void* nvEncRegisterResource;
        public void* nvEncUnregisterResource;
        public void* nvEncReconfigureEncoder;
        public void* reserved1;
        public void* nvEncCreateMVBuffer;
        public void* nvEncDestroyMVBuffer;
        public void* nvEncRunMotionEstimationOnly;
        public void* nvEncGetLastErrorString;
        public void* nvEncSetIOCudaStreams;
        public void* nvEncGetEncodePresetConfigEx;
        public void* nvEncGetSequenceParamEx;
        public void* nvEncRestoreEncoderState;
        public void* nvEncLookaheadPicture;
        public fixed ulong reserved2[275];

        public static NV_ENCODE_API_FUNCTION_LIST Create()
        {
            return new NV_ENCODE_API_FUNCTION_LIST
            {
                version = StructVersion(2),
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Helper Functions
    // ═══════════════════════════════════════════════════════════════

    public const uint NVENCAPI_MAJOR_VERSION = 12;
    public const uint NVENCAPI_MINOR_VERSION = 2;

    public static uint GetApiVersion()
        => NVENCAPI_MAJOR_VERSION | (NVENCAPI_MINOR_VERSION << 24);

    /// <summary>
    /// Builds the version uint embedded in every NVENC structure.
    /// Format: bits[31:28]=0x7, bits[27:24]=minor, bits[23:16]=ver, bits[15:0]=major
    /// </summary>
    public static uint StructVersion(uint structVer)
        => (0x7u << 28) | (structVer << 16) | GetApiVersion();

    // ═══════════════════════════════════════════════════════════════
    // NVENC API Loader
    // ═══════════════════════════════════════════════════════════════

    [DllImport(NvEncDll, EntryPoint = "NvEncodeAPICreateInstance")]
    public static extern NVENCSTATUS NvEncodeAPICreateInstance(
        NV_ENCODE_API_FUNCTION_LIST* functionList);

    [DllImport(NvEncDll, EntryPoint = "NvEncodeAPIGetMaxSupportedVersion")]
    public static extern NVENCSTATUS NvEncodeAPIGetMaxSupportedVersion(uint* version);
}
