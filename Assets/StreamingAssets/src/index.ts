import { init, KeyFrame, WaveType } from "./api";

const initialFrame: KeyFrame = {
  sampleRate: 10,
  thickness: 1.2,
  time: 4,
  duration: 4,
  waves: [{ frequency: 1, amplitude: 1, waveType: WaveType.Sine, phaseDegrees: 90, displacementAxis: { x: 1, y: 0, z: 0 } }]
};

init(initialFrame);