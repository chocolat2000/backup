package crypto

import (
	"encoding/binary"

	"github.com/spaolacci/murmur3"
)

// Murmur3Hash generates a 128-bit MurmurHash3 hash.
// It returns a 16-byte array
func Murmur3Hash(data []byte, seed uint32) []byte {
	hasher := murmur3.New128WithSeed(seed)
	hasher.Write(data)

	h1, h2 := hasher.Sum128()

	result := make([]byte, 16)
	binary.LittleEndian.PutUint64(result[0:8], h1)
	binary.LittleEndian.PutUint64(result[8:16], h2)

	return result
}
