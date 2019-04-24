<template>
  <div id="app">
    <div
      v-if="isConnected === false"
      class="error-message"
      >
      Disconnected
      <span v-if="isConnectedFailureReason !== ''"> - {{ isConnectedFailureReason }}</span>.
      <a href="#" @click="connect">Reconnect?</a>
    </div>
    <div class="container">
      <div class="sql-list">
        <div
          v-for="(sql, index) in dataView"
          :key="index"
          @click="statementSelected(sql)"
          class="sql-item"
        >
          <pre>{{ sql.sqlStatement }}</pre>
          <ul class="meta">
            <li class="long">{{ sql.occuredAt | formatDate }}</li>
            <li>Rows: {{ sql.estimatedRows }}</li>
            <li>Cost: {{ sql.estimatedCost }}</li>
          </ul>
        </div>
      </div>
      <div class="plan">
        <statement-view
          v-if="selectedStatement !== undefined"
          :show-plan="getPlan(selectedStatement)">
        </statement-view>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { Component, Vue } from 'vue-property-decorator';
import * as signalR from '@aspnet/signalr';
import { format, isSameDay, parseISO } from 'date-fns';
import 'showplan-vue/dist/showplan-vue.css';
import { ShowPlanParser, ShowPlanXML } from 'showplan-js';
import { Statement as StatementView } from 'showplan-vue';
import { showplanMessage } from './showPlanMessage';

@Component({
  components: {
    StatementView,
  },
  filters: {
    formatDate(input: string) {
      const date = parseISO(input);
      const formatString = (isSameDay(new Date(), date)) ? 'HH:mm:ss' : 'yyyy/MM/dd HH:mm:ss';
      return format(date, formatString);
    },
  },
  data() {
    return {
      selectedStatement: undefined,
    };
  },
})
export default class App extends Vue {
  sqlStatements: showplanMessage[] = [];

  parsedPlans = new Map<string, ShowPlanXML>();

  selectedStatement?: showplanMessage = undefined;

  isConnected = false;

  isConnectedFailureReason: string = '';

  $refs!: {
    planFrame: HTMLFrameElement
  }

  data() {
    return {
      selectedStatement: undefined,
    };
  }

  get dataView(): showplanMessage[] {
    return this.sqlStatements
      .sort((a, b) => new Date(b.occuredAt).getTime() - new Date(a.occuredAt).getTime());
  }

  getPlan(message: showplanMessage) {
    if (this.parsedPlans.has(message.queryPlanHashString) === false) {
      this.parsedPlans.set(message.queryPlanHashString, ShowPlanParser.Parse(message.showplan));
    }

    return this.parsedPlans.get(message.queryPlanHashString);
  }

  statementSelected(statement: showplanMessage) {
    this.selectedStatement = statement;
  }

  mounted() {
    this.connect();
  }

  private connect() {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:11188/showPlanHub')
      .build();

    const maxLength = 250;

    connection.on('ReceivePlan', (message: showplanMessage) => {
      let newStatementList = this.sqlStatements.concat(message);

      // don't want this list growing too big, but we will
      // only concat if the is grows 110% bigger than the max
      // to prevent from wiping and rebuilding all the time
      if (newStatementList.length > maxLength * 1.1) {
        newStatementList = newStatementList
          .sort((a, b) => new Date(b.occuredAt).getTime() - new Date(a.occuredAt).getTime())
          .slice(0, maxLength);

        // clear out the parsed plans cache while we are at it
        this.parsedPlans = new Map<string, ShowPlanXML>();
      }

      this.sqlStatements = newStatementList;
    });

    connection.onclose((err) => {
      this.isConnected = false;
      if (err !== undefined) {
        this.isConnectedFailureReason = err.toString();
      }
    });

    connection.start().then(() => {
      this.isConnected = true;
    }).catch((err) => {
      this.isConnected = false;
      this.isConnectedFailureReason = err.toString();
    });
  }
}
</script>

<style lang="scss" scoped>

.container {
  display: flex;

  .sql-list {
    flex: 1 0 0;
  }

  .plan {
    flex: 4 0 0;
    overflow: auto;
    padding: 0 1rem;
    height: 100vh;
  }
}

#app {
  font-family: 'Segoe UI', Verdana, Helvetica, Arial, sans-serif;
  color: #2c3e50;

  --background: #fff;
  --foreground: rgba(0, 0, 0, 0.8);
  --alt-background: #F1F1EF;
  --border: rgba(0,0,0,.25);
  --alt-border: rgba(0,0,0,.2);
  --red: #A71D5D;
  --blue: #183691;
  --green: #63a35c;
  --grey: #969896;
  --brown: #75715E;
  --orange: #df5000;
  --purple: #795da3;
  --light-blue: #445588;
}

.sql-list {
  height: 100vh;
  overflow-y: scroll;
}

.sql-item {

  font-size:.8rem;

  pre {
    font-family: Consolas,Courier New,Courier,monospace;
    margin: .5rem;
    max-height: 10rem;
    overflow: hidden;
  }

  ul.meta {
    border-bottom: 1px solid #ccc;
    border-top: 1px solid #ccc;

    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    background-color: #eee;

    li {
      flex: 1;
      display: inline;
      margin:.5rem;

      &.long {
        flex: 3;
      }
    }
  }
}

</style>
