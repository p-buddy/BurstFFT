### 12 / 11 / 22

In order to not getting any audio clipping or ducking when the synthesizer switches wave settings, 
we need a way to effectively switch between the wave settings.

I think a good idea is to just "hard" switch when both settings cross zero. 

There's the imperfect but easy to implement way to do this discretely by capturing the previous sample and when they switch signs (or one equals zero),
we switch to the next setting. 

The other way would be to use math! However, this can be tricky, since though we can determine when a specific wave crosses zero:

```ts
// ts
const wave = {
    /// in radians
    phase: number,
    frequency: number,
    getZeroCrossingX: (n: number) => {
        const pi = 3.14; // use actual pi value
        return (n * pi - this.phase) / (2 * pi * this.frequency);
    }
}
```

Wavynth waves are lerping in between two wave settings at once, so you actually need to figure out when this morphing wave crosses 0. 

Also, at the point, it might be worth it to just calculate when the two wave 'morphs' equal each other.