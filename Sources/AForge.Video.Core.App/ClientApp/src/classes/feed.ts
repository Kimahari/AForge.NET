export enum feedType {
    ipCam,
    usbFeed
}

export interface IDevice {
    id: number,
    monikerString: string;
    name: string;
}

export interface IDeviceRequest {
    address?: string;
    type?: number;
}

export interface IFeed {
    fps: number;
    address: string;
    enabled: boolean;
    id: number;
    status: string;
    visable: boolean;
    image: any;
    type: number;
}
