
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

export let x: 4;

export class X {
    
}

class A {
    internal: any;

    get language(): number {
        return this.internal._language;
    }

    constructor(time: 4) {
        // @ts-ignore
        this.internal = new A_internal(time);
    }

    doSomething = (): number => this.internal.doSomething();
    doSomethingElse = (x: number): number => this.internal.doSomethingElse(x);
}

// A_internal is a registered type that knows how to convert the javascript dyno object args to what c# wants

const y = new A(4);
y.doSomethingElse(y.language);
