
export type KeyFrame = {
    sampleRate: number;
    time: number;
    thickness: number;
    duration: number;
    waves: WaveState[];
};
export type WaveState = {
    frequency: number;
    amplitude: number;
    waveType: WaveType;
    phaseDegrees: number;
    displacementAxis: SimpleFloat3;
};
export enum WaveType {
    Sine = 'Sine',
    Square = 'Square',
    Triangle = 'Triangle',
    Sawtooth = 'Sawtooth',
}
export type SimpleFloat3 = {
    x: number;
    y: number;
    z: number;
};


export const init = (frame: KeyFrame): void => {
    // @ts-ignore
    initInternal(frame);
}

export const addAt = (frame: KeyFrame, time: number): void => {
    // @ts-ignore
    addAtInternal(frame, time);
};