<template>
  <div class="paginationWrapper">
    <div class="pageCount">
      <span>{{ getViewedPages }}</span>
    </div>

    <div class="pageNavControl" v-if="totalResults > 0">
      <div :class="navArrowClass('prev')" @click="prevOrNextPage('prev')">
        <IconPrevArrow />
      </div>
      <ul class="pageNavNumberButtons">
        <li
          v-if="currentPage > 3"
          :class="pageNumClass(1)"
          @click="goToPage(1)"
        >
          1
        </li>
        <span v-if="currentPage > 3">...</span>

        <li
          v-for="num in navPageNumbers"
          :key="`pagenum-${num}`"
          :class="pageNumClass(num)"
          @click="goToPage(num)"
        >
          {{ num }}
        </li>

        <span v-if="currentPage < totalPages - 2">...</span>
        <li
          v-if="currentPage < totalPages - 2"
          :class="pageNumClass(totalPages)"
          @click="goToPage(totalPages)"
        >
          {{ totalPages }}
        </li>
      </ul>
      <div :class="navArrowClass('next')" @click="prevOrNextPage('next')">
        <IconNextArrow />
      </div>
    </div>

    <div class="perPageControl" v-if="totalResults > 0">
      <span>Display</span>
      <ul class="perPageAmountList">
        <li
          v-for="amount in perPageAmounts"
          :key="`amt--${amount}`"
          :class="`perPageAmount ${
            resultsPerPage === amount ? 'currentAmount' : ''
          }`"
          @click="changePerPageAmount(amount)"
        >
          {{ amount }}
        </li>
      </ul>
    </div>
  </div>
</template>
<script>
import IconPrevArrow from '../assets/svg/fi_prev-arrow.svg?inline'
import IconNextArrow from '../assets/svg/fi_next-arrow.svg?inline'

/**
 * Emits:
 * - - event: amount-change
 * - - payload: new amount of items per page
 *
 * - - event: page-change
 * - - payload: new page number
 */

export default {
  components: {
    IconPrevArrow,
    IconNextArrow,
  },
  props: {
    totalResults: {
      type: Number,
      default: 0,
    },
    currentPerPage: {
      type: Number,
      default: 10,
    },
    currentPage: {
      type: Number,
      default: 1,
    },
  },

  data() {
    return {
      resultsPerPage: 10,
      perPageAmounts: [10, 25, 50, 100],
    }
  },

  computed: {
    totalPages() {
      return Math.ceil(this.totalResults / this.resultsPerPage)
    },
    getViewedPages() {
      if (this.totalResults === 0) return `0 results`

      const startingIndex = (this.currentPage - 1) * this.resultsPerPage + 1
      const endingIndex = startingIndex + this.resultsPerPage - 1
      const pIndex =
        startingIndex === endingIndex
          ? startingIndex
          : `${startingIndex}-${
              endingIndex > this.totalResults ? this.totalResults : endingIndex
            }`
      return `${pIndex} of ${this.totalResults} results`
    },
    navPageNumbers() {
      // if less than 3 pages, return array of N pages
      if (this.totalPages < 3)
        return Array.from(Array(this.totalPages), (_, index) => index + 1)

      // if page is less than 3, return first 3 pages
      if (this.currentPage < 3) return [1, 2, 3]

      // if page is 3 or more, return page - 1, page, page + 1
      if (this.currentPage >= 3 && this.currentPage < this.totalPages - 2)
        return [this.currentPage - 1, this.currentPage, this.currentPage + 1]

      // if total pages - page is 3 or less, return last 3 pages
      return [this.totalPages - 2, this.totalPages - 1, this.totalPages]
    },
  },

  mounted() {
    this.init()
  },

  methods: {
    init() {
      this.resultsPerPage = this.currentPerPage

      const data = {
        page: 1,
        size: this.resultsPerPage,
      }

      this.$emit('pagination-mounted', data)
    },
    changePerPageAmount(amount) {
      this.handleAmountChange(amount)
    },
    navArrowClass(direction) {
      if (direction === 'prev') {
        return `${direction}Page ${this.currentPage === 1 ? 'disabled' : ''}`
      } else {
        return `${direction}Page ${
          this.currentPage === this.totalPages ? 'disabled' : ''
        }`
      }
    },
    goToPage(pageNum) {
      this.handlePageChange(pageNum, this.resultsPerPage)
    },
    pageNumClass(num) {
      return `pageNavNumberButton ${
        this.currentPage === num ? 'currentPage' : ''
      }`
    },
    prevOrNextPage(page) {
      if (page === 'prev' && this.currentPage > 1) {
        this.handlePageChange(this.currentPage - 1, this.resultsPerPage)
      } else if (page === 'next' && this.currentPage < this.totalPages) {
        this.handlePageChange(this.currentPage + 1, this.resultsPerPage)
      }
    },
    handlePageChange(page, size) {
      if (this.currentPage === page && this.resultsPerPage === size) {
        return
      }

      this.resultsPerPage = size

      /* this.currentPage = page */

      const data = { page, size: this.resultsPerPage }

      this.$emit('page-change', data)
    },
    handleAmountChange(size) {
      this.handlePageChange(1, size)
    },
  },
}
</script>
<style lang="scss" scoped>
.paginationWrapper {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: 24px;
}

.pageNavControl {
  display: flex;
  gap: 16px;

  .prevPage,
  .nextPage {
    cursor: pointer;
    width: 32px;
    height: 32px;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 40px;
    background: $brand-white;
    transition: all 0.2s linear;

    &:active {
      background: $brand-color-scale-1;
    }

    svg {
      path {
        stroke: $brand-color-scale-6;
      }
    }

    &.disabled {
      cursor: none;
      pointer-events: none;
      background: $brand-color-scale-1;

      svg {
        path {
          stroke: $brand-white;
        }
      }
    }
  }
}

.pageNavNumberButtons {
  display: flex;
  align-items: center;
  gap: 16px;
}

.pageNavNumberButton {
  cursor: pointer;
  width: 32px;
  height: 32px;
  border-radius: 32px;

  font-family: $font-opensans;
  font-size: 14px;
  line-height: 16px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;

  color: $brand-color-scale-6;
  background: $brand-color-scale-1;
  transition: all 0.1s linear;

  &.currentPage {
    background: $brand-primary-color;
    color: $brand-white;
  }
}

.perPageControl {
  display: flex;
  align-items: center;
  gap: 16px;
}

.perPageAmountList {
  display: flex;
  gap: 16px;
  padding: 0 0 0 0;
  margin: 0 0 0 0;
  align-items: center;
  justify-content: center;
}

.perPageAmount {
  cursor: pointer;
  width: 32px;
  height: 32px;
  border-radius: 32px;

  font-family: $font-opensans;
  font-size: 12px;
  line-height: 14px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;

  color: $brand-color-scale-6;
  background: $brand-color-scale-1;
  transition: all 0.1s linear;

  &.currentAmount {
    background: $brand-primary-color;
    color: $brand-white;
  }
}
</style>
