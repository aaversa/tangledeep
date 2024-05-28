#include <nn/nn_Log.h>
#include <nn/fs/fs_File.h>

extern "C" {

	void TestPenif( const char* words)
	{
		NN_LOG(words);
	}
	
	void ReadFileChunk(nn::fs::FileHandle handle, int64_t offset, void* buffer, size_t size, int64_t buffer_offset)
	{
		nn::fs::ReadFile(handle, offset, (uint8_t*)buffer + buffer_offset, size);
	}
	
}