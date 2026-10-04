import { VALIDATION_POLICY as P } from './validation-policy';

export async function imageError(file: File): Promise<string | null> {
  const type = file.type.toLowerCase();
  if (!['image/jpeg', 'image/jpg', 'image/png', 'image/webp'].includes(type)) return 'validation.imageType';
  if (!file.size || file.size > P.ImageMaxBytes) return 'validation.imageSize';
  try {
    const bytes = new Uint8Array(await file.arrayBuffer());
    const text = (start: number, length: number) => String.fromCharCode(...bytes.slice(start, start + length));
    const valid = type === 'image/png'
      ? bytes.length >= 45 && [137,80,78,71,13,10,26,10].every((value, index) => bytes[index] === value) && text(12, 4) === 'IHDR' && text(bytes.length - 8, 4) === 'IEND'
      : type === 'image/webp'
        ? bytes.length >= 20 && text(0, 4) === 'RIFF' && text(8, 4) === 'WEBP' && new DataView(bytes.buffer).getUint32(4, true) === bytes.length - 8
        : bytes.length >= 4 && bytes[0] === 255 && bytes[1] === 216 && bytes[2] === 255 && bytes[bytes.length - 2] === 255 && bytes[bytes.length - 1] === 217;
    return valid ? null : 'validation.imageContent';
  } catch { return 'validation.imageContent'; }
}
