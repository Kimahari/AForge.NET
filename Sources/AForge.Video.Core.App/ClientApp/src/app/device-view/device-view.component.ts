import { HttpClient } from '@angular/common/http';
import { Component, EventEmitter, OnInit, Output, Input } from '@angular/core';
import { IDevice, IDeviceRequest, feedType, IFeed } from '../../classes/feed';

@Component({
    selector: '[app-device-view]',
    templateUrl: './device-view.component.html',
    styleUrls: ['./device-view.component.css']
})
export class DeviceViewComponent implements OnInit {
    @Input() feed: IFeed;

    sendingSignal = false;

    constructor(private client: HttpClient) {

    }

    ngOnInit(): void {

    }

    async startStopFeed() {
        const cmd = this.feed.enabled ? 'Stop' : 'Start';
        this.sendingSignal = true;
        try {
            await this.client.post(`api/feed/${this.feed.id}/${cmd}`, {}).toPromise();
        } catch (error) {

        } finally {
            this.sendingSignal = false;
        }
    }
}
