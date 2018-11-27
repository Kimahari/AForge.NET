import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { IFeed } from '../classes/feed';

import * as signalR from "@aspnet/signalr";
import { DomSanitizer } from '@angular/platform-browser';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {

    title = 'app';
    feeds: IFeed[];

    constructor(private client: HttpClient, private ss: DomSanitizer) {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/feedupdates")
            .configureLogging(signalR.LogLevel.Information)
            .build();

        connection.on('feedUpdate', (oo) => {
            const related = this.feeds.find(ii => ii.id == oo.id);
            related.image = this.ss.bypassSecurityTrustResourceUrl(`data:image/jpeg;base64, ${oo.data}`);
        });

        connection.on('fpsUpdate', (oo) => {
            const related = this.feeds.find(ii => ii.id == oo.id);
            related.fps = oo.frameRate;
        });

        connection.start();
    }

    async ngOnInit() {
        try {
            this.feeds = await this.client.get<IFeed[]>('/api/feed').toPromise();
            (this.feeds || []).forEach(async oo => {
                oo.visable = true;
                await this.onRefresh(oo);
            });
        } catch (error) {
            console.error(error);
        }

    }

    async onRefresh(oo: IFeed) {
        const data = await this.client.get<string>(`api/feed/${oo.id}`).toPromise();
        oo.image = this.ss.bypassSecurityTrustResourceUrl(`data:image/jpeg;base64, ${data}`);
    }
}
