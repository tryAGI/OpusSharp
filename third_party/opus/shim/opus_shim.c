#include <stdint.h>
#include <opus.h>

// Non-variadic shim functions to safely call opus_encoder_ctl from .NET on platforms
// where calling C varargs via P/Invoke is not supported (notably macOS arm64).

#ifdef __cplusplus
extern "C" {
#endif

__attribute__((visibility("default")))
int opussharp_encoder_set_int(OpusEncoder* st, int request, int32_t value) {
    return opus_encoder_ctl(st, request, value);
}

__attribute__((visibility("default")))
int opussharp_encoder_get_int(OpusEncoder* st, int request, int32_t* value) {
    return opus_encoder_ctl(st, request, value);
}

__attribute__((visibility("default")))
int opussharp_decoder_set_int(OpusDecoder* st, int request, int32_t value) {
    return opus_decoder_ctl(st, request, value);
}

__attribute__((visibility("default")))
int opussharp_decoder_get_int(OpusDecoder* st, int request, int32_t* value) {
    return opus_decoder_ctl(st, request, value);
}

#ifdef __cplusplus
}
#endif
