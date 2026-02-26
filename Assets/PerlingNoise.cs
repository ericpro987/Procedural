using UnityEngine;

namespace m17
{
    public class PerlinNoise
    {
        private PerlinNoise() { }

        public static float CalculatePerlinNoise(float x, float y, float frequency, int width, int height, float offsetX = 0, float offsetY = 0,
            int octaves = 0, float lacunarity = 2, float persistence = 0.5f, bool carveOctaves = true,
            bool verbose = false)
        {
            return CalculatePerlinNoise(x, y, frequency, width, height, offsetX, offsetY, octaves, lacunarity, persistence, carveOctaves, verbose, false)[1];
        }

        public static float[] CalculatePerlinNoise(float x, float y, float frequency, int width, int height, float offsetX = 0, float offsetY = 0,
            int octaves = 0, float lacunarity = 2, float persistence = 0.5f, bool carveOctaves = true,
            bool verbose = false, bool returnAllValues = false)
        {
            float[] output = new float[2 + 2 * octaves];
            //Calculem el nostre pas donada la frequencia (que realment �s tractada com a periode)
            float step = frequency / Mathf.Max(width, height);
            //Per cada casella comprovem soroll perlin donats els par�metres
            // les coordenades x i y que buscarem venen despla�ades per l'offset
            // la freq�encia ens determina com de grans s�n els passos que fem
            float xCoord = offsetX + x * step;
            float yCoord = offsetY + y * step;
            float sample = Mathf.PerlinNoise(xCoord, yCoord);
            output[0] = sample;
            //Valor base
            if (verbose) Debug.Log($"Base: [{x},{y}] = {sample}");

            //Acte seguit calculem les octaves
            for (int octave = 1; octave <= octaves; octave++)
            {
                //La Lacunarity afecta a la freq�encia de cada subseq�ent octava. El limitem a [2,3] de forma
                // que cada nou valor sigui 1/2 o 1/3 de la freq�encia anterior (doble o triple de soroll)
                float newStep = frequency * lacunarity * octave / Mathf.Max(width, height);
                float xOctaveCoord = offsetX + x * newStep;
                float yOctaveCoord = offsetY + y * newStep;

                //valor base de l'octava
                float octaveSample = Mathf.PerlinNoise(xOctaveCoord, yOctaveCoord);
                output[2 * octave] = octaveSample;

                //La Persistence afecta a l'amplitud de cada subseq�ent octava. El limitem a [0.1, 0.9] de forma
                // que cada nou valor afecti menys al resultat final.
                //Si Carve Octaves est� actiu ->
                // addicionalment, farem que el soroll en comptes de ser un valor base [0,1] sigui [-0.5f,0.5f]
                // i aix� pugui sumar o restar al valor inicial
                octaveSample = (octaveSample - (carveOctaves ? .5f : 0)) * (persistence / octave);

                //acumulaci� del soroll amb les octaves i base anteriors
                if (verbose) Debug.Log($"Octave {octave}: [{x},{y}] = {octaveSample}");
                sample += octaveSample;
                output[2 * octave + 1] = sample;
            }

            output[1] = sample;
            if (verbose) Debug.Log($"Post octaves: [{x},{y}] = {sample}");

            return output;
        }
    }
}


