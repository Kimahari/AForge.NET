import { Component, OnInit, Input, EventEmitter, Output } from '@angular/core';
import { IDevice, IDeviceRequest } from '../../classes/feed';
import { HttpClient } from '@angular/common/http';

@Component({
    selector: '[app-add-device]',
    templateUrl: './add-device.component.html',
    styleUrls: ['./add-device.component.css']
})
export class AddDeviceComponent implements OnInit {
    loadingDevices: boolean;
    devices: IDevice[];
    addingDevice = false;

    deviceRequest: IDeviceRequest = {};

    @Output() deviceAdded = new EventEmitter();

    constructor(private client: HttpClient) { }

    ngOnInit() {

    }

    get valid() {
        if (this.addingDevice) return false;
        if (this.feedType < 0) return false;
        if (!this.deviceRequest.address) return false;
        return true;
    }

    private _selectedDevice = -1;

    public get selectedDevice(): number {
        return this._selectedDevice;
    }
    public set selectedDevice(v: number) {
        this._selectedDevice = +v;

        if (this._selectedDevice == -1)
            this.deviceRequest.address = '';

        let device = this.devices.find(oo => oo.id == this._selectedDevice);
        this.deviceRequest.address = device && device.monikerString || '';
    }

    public get feedType(): number {
        return this.deviceRequest.type;
    }
    public set feedType(v: number) {
        this.deviceRequest.type = v != undefined ? +v : v;
        this.deviceRequest.address = '';
        if (v == 1) {
            this.onLoadVideoDevices()
        }
    }

    async  onLoadVideoDevices() {
        this.loadingDevices = true;
        this.devices = (await this.client.get<any[]>('/api/feed/devices').toPromise()).map((oo: IDevice, index) => {
            return <IDevice>{
                id: index,
                name: oo.name,
                monikerString: oo.monikerString
            }
        });
        this.loadingDevices = false;
    }

    async onAddDevice() {
        try {
            this.addingDevice = true;
            await this.client.post(`/api/feed/devices`, this.deviceRequest).toPromise();
            this.deviceAdded.emit();
            this.deviceRequest = {};
        } catch (error) {

        } finally {
            this.addingDevice = false;
        }
    }

}
