<template>
  <div class="issueSummary">
    <FormLabel v-if="label" :label="label" :label-id="labelId"></FormLabel>
    <div class="issuesWrap">
      <div class="issues-col vulns">
        <div class="issues-col-label">Vulnerabilities</div>
        <ul>
          <li>
            <div :class="`severity-color critical`"></div>
            <p>Critical</p>
            <span>{{ summary.critical }}</span>
          </li>
          <li>
            <div :class="`severity-color high`"></div>
            <p>High</p>
            <span>{{ summary.high }}</span>
          </li>
          <li>
            <div :class="`severity-color medium`"></div>
            <p>Medium</p>
            <span>{{ summary.medium }}</span>
          </li>
          <li>
            <div :class="`severity-color low`"></div>
            <p>Low</p>
            <span>{{ summary.low }}</span>
          </li>
        </ul>
        <div class="col-total">
          <p>Total</p>
          <span>{{ firstTierTotal }}</span>
        </div>
      </div>

      <div class="issues-col info">
        <div class="issues-col-label">Info</div>
        <ul>
          <li>
            <div :class="`severity-color info`"></div>
            <p>Info</p>
            <span>{{ summary.info }}</span>
          </li>
        </ul>
        <div class="col-total">
          <p>Total</p>
          <span>{{ secondTierTotal }}</span>
        </div>
      </div>

      <div class="issues-summary-separator"></div>

      <div class="issues-chart">
        <div class="issues-chart-container">
          <div class="chart-bar-wrap">
            <div
              :style="`height:${summary.criticalBar}%`"
              :class="`chart-bar critical`"
            ></div>
            <div class="chart-initial">C</div>
          </div>
          <div class="chart-bar-wrap">
            <div
              :style="`height:${summary.highBar}%`"
              :class="`chart-bar high`"
            ></div>
            <div class="chart-initial">H</div>
          </div>
          <div class="chart-bar-wrap">
            <div
              :style="`height:${summary.mediumBar}%`"
              :class="`chart-bar medium`"
            ></div>
            <div class="chart-initial">M</div>
          </div>
          <div class="chart-bar-wrap">
            <div
              :style="`height:${summary.lowBar}%`"
              :class="`chart-bar low`"
            ></div>
            <div class="chart-initial">L</div>
          </div>
          <div class="chart-bar-wrap">
            <div
              :style="`height:${summary.infoBar}%`"
              :class="`chart-bar info`"
            ></div>
            <div class="chart-initial">I</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import uniqueId from 'lodash/uniqueId'
import FormLabel from './controls/FormLabel'

export default {
  components: {
    FormLabel,
  },
  props: {
    label: {
      type: String,
      default: 'Total Active Issues',
    },
    summary: {
      type: Object,
      required: true,
      default: () => ({
        critical: 0,
        high: 0,
        medium: 0,
        low: 0,
        info: 0,
        criticalBar: 0,
        highBar: 0,
        mediumBar: 0,
        lowBar: 0,
        infoBar: 0,
      }),
    },
    issues: {
      type: Array,
      default: () => [
        {
          label: 'Critical',
          slug: 'critical',
          qty: '10',
          value: '50',
        },
        {
          label: 'High',
          slug: 'high',
          qty: '18',
          value: '89',
        },
        {
          label: 'Medium',
          slug: 'medium',
          qty: '5',
          value: '24',
        },
        {
          label: 'Low',
          slug: 'low',
          qty: '12',
          value: '50',
        },
        {
          label: 'Info',
          slug: 'info',
          qty: '10',
          value: '30',
        },
        {
          label: 'Best Practice',
          slug: 'best-practice',
          qty: '18',
          value: '14',
        },
      ],
    },
  },
  computed: {
    labelId() {
      return uniqueId('issues-')
    },
    firstTierTotal() {
      const { critical, high, medium, low } = this.summary
      return (
        parseInt(critical) + parseInt(high) + parseInt(medium) + parseInt(low)
      )
    },
    secondTierTotal() {
      const { info } = this.summary
      return parseInt(info)
    },
  },
  methods: {
    getInitials(txt) {
      return txt
        .split(' ')
        .map((word) => word[0])
        .join('')
    },
  },
}

// 10 = 50.567%
// 18 = 89.413%
// 5  = 24.182%
// 12 = 50.567%
// 10 = 30.778%
// 18 = 13.923%
</script>

<style lang="scss" scoped>
.issueSummary {
  display: flex;
  flex-direction: column;
  padding: 0;
  margin: 0;
  background: none;
  max-width: 505px;
  gap: 8px;
}

.issuesWrap {
  position: relative;
  display: flex;
  padding: 16px;
  background: $brand-white;
  border: 1px solid $brand-color-scale-2;
  gap: 24px;
  flex: 1;
  min-height: 155px;
  border-radius: 8px;
}

.issues-col {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  justify-content: space-between;
  flex: 2;
  padding: 0;
  margin: 0;
  width: 100px;
  max-width: 110px;
  gap: 4px;

  .issues-col-label {
    flex: 0 0 auto;
    width: 100%;
    padding: 0 0 0 0;
    margin: 0 0 0 0;
    font-size: 10px;
    line-height: 16px;
    font-weight: 700;
    color: $brand-color-scale-6;
    font-family: $font-opensans;
  }

  ul {
    width: 100%;
    flex: 1;
    list-style: none;
    padding: 0 0 0 0;
    margin: 0 0 0 0;
    display: flex;
    flex-flow: column;
    height: 100%;
    position: relative;
    justify-content: flex-start;

    li {
      position: relative;
      display: flex;
      flex-flow: row;
      align-items: center;
      justify-content: flex-start;
      gap: 4px;
      flex: 0 0 auto;
      width: 100%;

      .severity-color {
        width: 8px;
        height: 8px;
        flex: 0 0 auto;
        border-radius: 25px;

        &.critical {
          background: $issue-severity-critical;
        }
        &.high {
          background: $issue-severity-high;
        }
        &.medium {
          background: $issue-severity-medium;
        }
        &.low {
          background: $issue-severity-low;
        }
        &.info {
          background: $issue-severity-info;
        }
        &.best-practice {
          background: $issue-severity-best-practice;
        }
      }

      p {
        flex: 1;
        font-size: 10px;
        font-weight: 400;
        color: $brand-color-scale-6;
        line-height: 16px;
        font-family: $font-opensans;
        text-align: left;
      }

      span {
        flex: 0 0 auto;
        font-size: 10px;
        line-height: 16px;
        font-weight: 700;
        font-family: $font-opensans;
        color: $brand-color-scale-6;
        align-self: flex-end;
      }
    }
  }

  .col-total {
    position: relative;
    display: flex;
    flex-flow: row;
    align-items: center;
    justify-content: space-between;
    flex: 0 0 auto;
    width: 100%;
    padding-top: 4px;
    border-top: 1px solid $brand-color-scale-2;

    p {
      flex: 1;
      font-size: 10px;
      font-weight: 400;
      color: $brand-color-scale-6;
      line-height: 16px;
      font-family: $font-opensans;
      text-align: left;
    }

    span {
      flex: 0 0 auto;
      font-size: 10px;
      line-height: 16px;
      font-weight: 700;
      font-family: $font-opensans;
      color: $brand-color-scale-6;
      align-self: flex-end;
    }
  }
}

.issues-summary-separator {
  width: 1px;
  background: $brand-color-scale-2;
  flex: 0 0 auto;
  position: relative;
}

.issues-chart {
  position: relative;
  flex: 1;
  min-height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;

  .issues-chart-container {
    width: 100%;
    height: 100%;
    display: flex;
    gap: 8px;
    justify-content: center;
  }

  .chart-bar-wrap {
    display: flex;
    flex-flow: column;
    justify-content: flex-end;
    position: relative;
    width: auto;
    height: 100%;
  }

  .chart-bar {
    flex: 0 0 auto;
    width: 100%;
    height: 100%;
    display: flex;
    flex-flow: column;
    justify-content: flex-end;
    border-radius: 16px 16px 0 0;
    transition: height 1s ease-out;

    &.critical {
      background: $issue-severity-critical;
    }
    &.high {
      background: $issue-severity-high;
    }
    &.medium {
      background: $issue-severity-medium;
    }
    &.low {
      background: $issue-severity-low;
    }
    &.info {
      background: $issue-severity-info;
    }
    &.best-practice {
      background: $issue-severity-best-practice;
    }
  }

  .chart-initial {
    display: flex;
    flex-flow: column;
    justify-content: center;
    align-items: center;
    width: 16px;
    height: 16px;
    flex: 0 0 auto;
    font-size: 10px;
    line-height: 20px;
    font-weight: 700;
    font-family: $font-opensans;
    color: $brand-color-scale-6;
  }
}
</style>
