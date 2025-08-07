#!/usr/bin/env node

/**
 * Generates dynamic benchmark badges from the latest benchmark results
 * These badges can be included in the README to show live performance data
 */

const fs = require('fs');
const path = require('path');

// Find the latest benchmark result files
function findLatestBenchmarkResults() {
    const benchmarkDir = path.join(__dirname, '../../benchmarks/HeroCsv.Benchmarks/BenchmarkDotNet.Artifacts/results');
    
    if (!fs.existsSync(benchmarkDir)) {
        console.log('No benchmark results found');
        return null;
    }

    const files = fs.readdirSync(benchmarkDir)
        .filter(f => f.endsWith('.json'))
        .map(f => ({
            name: f,
            path: path.join(benchmarkDir, f),
            mtime: fs.statSync(path.join(benchmarkDir, f)).mtime
        }))
        .sort((a, b) => b.mtime - a.mtime);

    return files.length > 0 ? files[0].path : null;
}

// Parse benchmark results and extract key metrics
function parseBenchmarkResults(jsonPath) {
    const data = JSON.parse(fs.readFileSync(jsonPath, 'utf8'));
    const benchmarks = data.Benchmarks || [];
    
    const results = {
        herocsv: {},
        competitors: {}
    };

    benchmarks.forEach(benchmark => {
        const method = benchmark.Method;
        const mean = benchmark.Statistics?.Mean || 0;
        const allocated = benchmark.Memory?.BytesAllocatedPerOperation || 0;
        
        if (method.includes('HeroCsv')) {
            if (method.includes('Factory')) {
                results.herocsv.factory = { mean, allocated };
            } else if (method.includes('Reflection')) {
                results.herocsv.reflection = { mean, allocated };
            } else if (method.includes('SourceGen')) {
                results.herocsv.sourcegen = { mean, allocated };
            }
        } else if (method.includes('CsvHelper')) {
            results.competitors.csvhelper = { mean, allocated };
        } else if (method.includes('Sep')) {
            results.competitors.sep = { mean, allocated };
        } else if (method.includes('Sylvan')) {
            results.competitors.sylvan = { mean, allocated };
        }
    });

    return results;
}

// Calculate performance ranking
function calculateRanking(results) {
    const allResults = [];
    
    // Add HeroCsv best result
    const herocsvBest = Math.min(
        results.herocsv.factory?.mean || Infinity,
        results.herocsv.sourcegen?.mean || Infinity
    );
    if (herocsvBest < Infinity) {
        allResults.push({ name: 'HeroCsv', mean: herocsvBest });
    }

    // Add competitors
    Object.entries(results.competitors).forEach(([name, data]) => {
        if (data.mean) {
            allResults.push({ name, mean: data.mean });
        }
    });

    allResults.sort((a, b) => a.mean - b.mean);
    
    const herocsvRank = allResults.findIndex(r => r.name === 'HeroCsv') + 1;
    return {
        rank: herocsvRank,
        total: allResults.length,
        fastest: allResults[0]?.name || 'Unknown',
        percentSlower: herocsvRank > 1 ? 
            ((allResults[herocsvRank - 1].mean - allResults[0].mean) / allResults[0].mean * 100).toFixed(1) : 
            0
    };
}

// Generate badge JSON for shields.io
function generateBadgeJson(results, ranking) {
    const badges = {
        performance: {
            schemaVersion: 1,
            label: 'Performance',
            message: ranking.rank <= 2 ? `Top ${ranking.rank}` : `#${ranking.rank} of ${ranking.total}`,
            color: ranking.rank === 1 ? 'brightgreen' : ranking.rank === 2 ? 'green' : 'blue'
        },
        speed: {
            schemaVersion: 1,
            label: 'Speed vs Fastest',
            message: ranking.rank === 1 ? 'Fastest' : `${ranking.percentSlower}% slower`,
            color: ranking.rank === 1 ? 'brightgreen' : parseFloat(ranking.percentSlower) < 10 ? 'green' : 'yellow'
        },
        aot: {
            schemaVersion: 1,
            label: 'AOT Performance',
            message: results.herocsv.factory && results.herocsv.reflection ? 
                `${(results.herocsv.reflection.mean / results.herocsv.factory.mean).toFixed(1)}x faster` : 
                'N/A',
            color: 'brightgreen'
        },
        memory: {
            schemaVersion: 1,
            label: 'Memory vs Reflection',
            message: results.herocsv.factory && results.herocsv.reflection ? 
                `${(100 - results.herocsv.factory.allocated / results.herocsv.reflection.allocated * 100).toFixed(0)}% less` : 
                'N/A',
            color: 'brightgreen'
        }
    };

    return badges;
}

// Main execution
function main() {
    const resultsPath = findLatestBenchmarkResults();
    if (!resultsPath) {
        console.log('No benchmark results found. Run benchmarks first.');
        return;
    }

    console.log(`Processing benchmark results from: ${resultsPath}`);
    
    const results = parseBenchmarkResults(resultsPath);
    const ranking = calculateRanking(results);
    const badges = generateBadgeJson(results, ranking);

    // Save badge JSON files
    const badgeDir = path.join(__dirname, '../../.github/badges');
    if (!fs.existsSync(badgeDir)) {
        fs.mkdirSync(badgeDir, { recursive: true });
    }

    Object.entries(badges).forEach(([name, data]) => {
        const badgePath = path.join(badgeDir, `${name}.json`);
        fs.writeFileSync(badgePath, JSON.stringify(data, null, 2));
        console.log(`Generated badge: ${badgePath}`);
    });

    // Generate summary for GitHub Actions
    const summary = {
        timestamp: new Date().toISOString(),
        ranking,
        results: {
            herocsv: results.herocsv,
            topCompetitor: results.competitors[ranking.fastest.toLowerCase()] || {}
        }
    };

    const summaryPath = path.join(badgeDir, 'benchmark-summary.json');
    fs.writeFileSync(summaryPath, JSON.stringify(summary, null, 2));
    console.log(`Generated summary: ${summaryPath}`);

    console.log(`\nPerformance Summary:`);
    console.log(`- Rank: #${ranking.rank} of ${ranking.total} libraries`);
    console.log(`- Fastest: ${ranking.fastest}`);
    if (ranking.rank > 1) {
        console.log(`- ${ranking.percentSlower}% slower than fastest`);
    }
}

// Run if called directly
if (require.main === module) {
    main();
}

module.exports = { parseBenchmarkResults, calculateRanking, generateBadgeJson };